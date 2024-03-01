/*
 MIT License

Copyright (c) 2024 Marcin Walczyk<marcinwal9@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System;
using System.Linq;

namespace MemberBender
{
	public sealed class BendableMember<ParentType> : IBendableMember, IEquatable<BendableMember<ParentType>?>
	{
		public delegate object? GetMethod(object? obj);
		public delegate void SetMethod(object? obj, object? val);

		public BendableMember(Type memberType, string name, GetMethod? getValue, SetMethod? setValue)
		{
			this.memberType = memberType;
			Name = name;

			getter = getValue;
			setter = setValue;
		}

		public int CompareTo(IBendableMember? other)
		{
			return string.Compare(Name, other?.Name);
		}

		public bool Equals(BendableMember<ParentType>? other)
		{
			return other is not null &&
				   Name == other.Name &&
				   Readable == other.Readable &&
				   Writeable == other.Writeable;
		}

		public override bool Equals(object? obj)
		{
			return obj is BendableMember<ParentType> other &&
				   Name == other.Name &&
				   Readable == other.Readable &&
				   Writeable == other.Writeable;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Readable, Writeable);
		}

		public string Name { get; }

		public bool Readable => getter is not null;
		public bool Writeable => setter is not null;

		public object? TryGetValue(object? parent)
		{
			if (!Readable || parent is not ParentType) return null;
			return getter!(parent);
		}

		public object? TryGetValue(ParentType parent)
		{
			if (!Readable) return null;
			return getter!(parent);
		}


		public object? TrySetValue(object? parent, object? value)
		{
			if (!Writeable || parent is not ParentType || !memberType.IsInstanceOfType(value)) return null;
			setter!(parent, value);
			return value;
		}

		public object? TrySetValue(ParentType parent, object? value)
		{
			if (!Writeable || !memberType.IsInstanceOfType(value)) return null;
			setter!(parent, value);
			return value;
		}


		public object? GetValue(object? parent)
		{
			if (!Readable) throw new NotSupportedException($"member \"{Name}\" does not support reading!");
			return getter!(parent);
		}

		public object? GetValue(ParentType parent)
		{
			if (!Readable) throw new NotSupportedException($"member \"{Name}\" does not support reading!");
			return getter!(parent);
		}


		public void SetValue(object? parent, object? value)
		{
			if (!Writeable) throw new NotSupportedException($"member \"{Name}\" does not support writing!");
			if (parent is not ParentType) throw new ArgumentException($"member \"{Name}\" is part of type: \"{typeof(ParentType).Name}\", but got type: \"{parent.GetType().Name}\" ");
			if (!memberType.IsInstanceOfType(value)) throw new ArgumentException($"member \"{Name}\" expects \"{memberType.Name}\"");
			setter!(parent, value);
		}

		public void SetValue(ParentType parent, object? value)
		{
			if (!Writeable) throw new NotSupportedException($"member \"{Name}\" does not support writing!");
			if (!memberType.IsInstanceOfType(value)) throw new ArgumentException($"member \"{Name}\" expects \"{memberType.Name}\"");
			setter!(parent, value);
		}


		private readonly Type memberType;
		private readonly GetMethod? getter;
		private readonly SetMethod? setter;
	}
}
