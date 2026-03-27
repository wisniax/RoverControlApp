using Godot;

[Tool]
public partial class DraggablePanel : Control
{
	[ExportGroup("References")]
	[Export] public Control DragArea = null!;
	[Export] public Label TitleLabel = null!;
	[Export] public Button CloseButton = null!;

	[ExportGroup("Appearance")]
    [Export] public string WindowTitle = "Window";
    [Export] public Color TitleBarColor = new Color(0.15f, 0.15f, 0.15f);
    [Export] public Color TitleTextColor = Colors.White;

    [ExportGroup("Close Button")]
    [Export] public Color CloseButtonBackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    [Export] public Color CloseButtonIconColor = Colors.White;

    [ExportGroup("Behavior")]
    [Export] public bool ClampToViewport = true;
    [Export] public bool BringToFrontOnDrag = true;
	[Export] public bool ShowTheCloseButton = true;
	[Export] public bool ReturnToStartOnClose = true;
	[Export] public bool AlwaysOnTop = false;

	private Control _sceneRoot = null!;
	private bool _dragging = false;
    private Vector2 _startPosition;

    public override void _Ready()
    {
        _sceneRoot = GetOwner<Control>();

        if (_sceneRoot == null)
            return;

		_startPosition = _sceneRoot.Position;

		if (!Engine.IsEditorHint() && CloseButton != null)
            CloseButton.Pressed += OnClosePressed;

		_sceneRoot.VisibilityChanged += OnVisibilityChange;
		UpdateVisuals();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            UpdateVisuals();
    }

    public override void _Input(InputEvent @event)
    {
        if (_sceneRoot == null || DragArea == null || !_sceneRoot.Visible)
            return;

		Rect2 dragRect = DragArea.GetGlobalRect();
		Rect2 rootRect = _sceneRoot.GetGlobalRect();
		Vector2 mousePos = GetGlobalMousePosition();

		bool mouseOver = dragRect.HasPoint(mousePos);
		bool mouseOverRoot = rootRect.HasPoint(mousePos);

		if (!_dragging && !mouseOverRoot)
            return;

		if (
			@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left
		) {
			if (mouseButton.Pressed && !IsPanelAbove(mouseButton))
            {
				if(mouseOver) {
					_dragging = true;
				}
				if (BringToFrontOnDrag) {
					var parent = _sceneRoot.GetParent();
					parent?.MoveChild(_sceneRoot, parent.GetChildCount() - 1);
				}
			}
            else if (!mouseButton.Pressed)
            {
                _dragging = false;
            }

		}

		if (@event is InputEventMouseMotion motion && _dragging)
        {
			_sceneRoot.Position += motion.Relative;

            if (ClampToViewport && !Engine.IsEditorHint())
                ClampToScreen();
        }
    }

	public bool IsPanelAbove(InputEventMouseButton mouse)
	{
		Rect2 mouseRect = new Rect2(mouse.GlobalPosition, 10, 10);

		var parent = _sceneRoot.GetParent();
		if (parent == null) return false;
	
		foreach (Node node in parent.GetChildren())
		{
			if (node == _sceneRoot) continue;
			if (node is Panel other && other.Visible == true) {
				DraggablePanel otherPanel = other.GetNode<DraggablePanel>(nameof(DraggablePanel));
				if (otherPanel != null && otherPanel.AlwaysOnTop == true)
					return true;

				Rect2 otherRect = other.GetGlobalRect();
				if (mouseRect.Intersects(otherRect))
					if(other.GetIndex() > _sceneRoot.GetIndex())
							return true;
			}
		}
		return false;
	}


	private void UpdateVisuals()
    {
        if (TitleLabel != null) {
            TitleLabel.Text = WindowTitle;
            TitleLabel.AddThemeColorOverride("font_color", TitleTextColor);
        }

        if (DragArea is ColorRect rect)
            rect.Color = TitleBarColor;

        if (CloseButton != null)
        {
			if(!ShowTheCloseButton) {
				CloseButton.Visible = false;
				return;
			}

            StyleBoxFlat style = new StyleBoxFlat();
            style.BgColor = CloseButtonBackgroundColor;

            CloseButton.AddThemeStyleboxOverride("normal", style);
            CloseButton.AddThemeStyleboxOverride("hover", style);
            CloseButton.AddThemeStyleboxOverride("pressed", style);

            CloseButton.AddThemeColorOverride("icon_normal_color", CloseButtonIconColor);
        }
    }

    private void OnClosePressed()
    {
        SetWindowVisible(false);
    }

	public void SetWindowVisible(bool value) {

		if (_sceneRoot == null)
            return;

		_sceneRoot.Visible = value;

		if (!value && ReturnToStartOnClose)
            _sceneRoot.Position = _startPosition;
    }

	public void OnVisibilityChange() {
		if (AlwaysOnTop && Visible == true) {
			var parent = _sceneRoot.GetParent();
			parent?.MoveChild(_sceneRoot, parent.GetChildCount() - 1);
		}
	}

	private void ClampToScreen()
    {
        var viewportSize = GetViewportRect().Size;

        _sceneRoot.Position = new Vector2(
            Mathf.Clamp(_sceneRoot.Position.X, 0, viewportSize.X - _sceneRoot.Size.X),
            Mathf.Clamp(_sceneRoot.Position.Y, 0, viewportSize.Y - _sceneRoot.Size.Y)
        );
    }

}
