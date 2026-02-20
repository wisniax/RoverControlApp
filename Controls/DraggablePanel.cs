using Godot;

[Tool]
public partial class DraggablePanel : Control
{
	[Signal]
	public delegate void PanelVisibilityChangedEventHandler();

	[ExportGroup("References")]
    [Export] public Control DragArea;
    [Export] public Label TitleLabel;
    [Export] public Button CloseButton;

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

	private Control _sceneRoot;
    private bool _dragging = false;
    private Vector2 _startPosition;

    public override void _Ready()
    {
        _sceneRoot = GetOwner() as Control;

        if (_sceneRoot == null)
            return;

		_startPosition = _sceneRoot.Position;

		if (!Engine.IsEditorHint() && CloseButton != null)
            CloseButton.Pressed += OnClosePressed;

        UpdateVisuals();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            UpdateVisuals();
    }

    public override void _Input(InputEvent @event)
    {
        if (_sceneRoot == null || DragArea == null)
            return;

        Vector2 mousePos = GetGlobalMousePosition();
        Rect2 dragRect = new Rect2(DragArea.GlobalPosition, DragArea.Size);
        bool mouseOver = dragRect.HasPoint(mousePos);

        if (!_dragging && !mouseOver)
            return;

        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed && mouseOver)
            {
                _dragging = true;

                if (BringToFrontOnDrag)
                {
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

    private void UpdateVisuals()
    {

        if (TitleLabel != null)
        {
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

	public void SetWindowVisible(bool value)
    {
        if (_sceneRoot == null)
            return;

		_sceneRoot.Visible = value;
		_sceneRoot.EmitSignal(SignalName.PanelVisibilityChanged);

		if (!value && ReturnToStartOnClose)
            _sceneRoot.Position = _startPosition;
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
