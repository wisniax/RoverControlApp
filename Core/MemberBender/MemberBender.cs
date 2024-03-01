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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MemberBender
{
	public static class MemberBender<ParentType>
	{
		private static SortedSet<BendableMember<ParentType>> Explore()
		{
			SortedSet<BendableMember<ParentType>> members = new();

			foreach (var field in typeof(ParentType).GetFields())
				members.Add(
					new BendableMember<ParentType>(
						field.FieldType,
						field.Name, 
						field.GetValue,
						field.SetValue
					)
				);

			foreach (var property in typeof(ParentType).GetProperties())
				members.Add(
					new BendableMember<ParentType>(
						property.PropertyType,
						property.Name,
						property.CanRead ? property.GetValue : null,
						property.CanWrite ?	property.SetValue : null
					)
				);

			return members;
		}

		public static BendableMember<ParentType>? Member(string name)
		{
			return members.FirstOrDefault( (bendable) => bendable.Name == name );
		}

		public static IEnumerable<BendableMember<ParentType>> Members() => members.AsEnumerable();

		private static readonly SortedSet<BendableMember<ParentType>> members = Explore();
	}
}
