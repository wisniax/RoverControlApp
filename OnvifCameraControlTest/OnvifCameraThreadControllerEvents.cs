namespace OnvifCameraControlTest.OnvifCameraThreadControllerEvents;

//simple example. dalej baw się sam
public static class OnvifCameraThreadControllerEvents
{
    public static void MoveLeft(this OnvifCameraThreadController controller)
    {
        controller.CameraMotion = CameraMotionState.PanLeft;
    }

    public static void MoveRight(this OnvifCameraThreadController controller)
    {
        controller.CameraMotion = CameraMotionState.PanRight;
    }

    public static void MoveUp(this OnvifCameraThreadController controller)
    {
        controller.CameraMotion = CameraMotionState.PanUp;
    }

    public static void MoveDown(this OnvifCameraThreadController controller)
    {
        controller.CameraMotion = CameraMotionState.PanDown;
    }

    public static void MoveStop(this OnvifCameraThreadController controller)
    {
        controller.CameraMotion = CameraMotionState.None;
    }

    public static void ZoomIn(this OnvifCameraThreadController controller)
    {
        controller.CameraZoom = CameraZoomState.ZoomIn;
    }

    public static void ZoomOut(this OnvifCameraThreadController controller)
    {
        controller.CameraZoom = CameraZoomState.ZoomOut;
    }

    public static void ZoomStop(this OnvifCameraThreadController controller)
    {
        controller.CameraMotion = CameraMotionState.None;
        controller.CameraZoom = CameraZoomState.None;
    }
}
