namespace Sandbox.Player;

public sealed class CameraMovement : Component
{
    [Property] public PlayerController Player { get; set; }
    [Property] public GameObject Body { get; set; }
    [Property] public GameObject Head { get; set; }
    [Property] public float Distance { get; set; } = 150f; // Start in third person

    public bool IsFirstPerson => Distance == 0f;
    private CameraComponent _camera;
    private ModelRenderer _bodyRenderer;
    public Vector3 CurrentOffset = Vector3.Zero;

    protected override void OnStart()
    {
        InitializeComponents();
        ApplyClothing(); // Ensure clothing is applied correctly for third person
    }

    protected override void OnUpdate()
    {
        HandleViewToggle();
        RotateHead();
        UpdateCurrentOffset();
        UpdateCameraPosition();
    }

    private void InitializeComponents()
    {
        _camera = Player.Components.GetInChildren<CameraComponent>();
        _bodyRenderer = Body.Components.Get<ModelRenderer>();
    }

    private void HandleViewToggle()
    {
        if (Input.Pressed("View"))
        {
            Distance = Distance == 0f ? 150f : 0f;
            ApplyClothing();
        }
    }

    private void ApplyClothing()
    {
        var model = Player.Components.GetInChildren<SkinnedModelRenderer>();
        if (model != null)
        {
            var clothingContainer = Distance == 0f ? new ClothingContainer() : ClothingContainer.CreateFromLocalUser();
            clothingContainer.Apply(model);
        }
    }

    private void RotateHead()
    {
        var eyeAngles = Head.Transform.Rotation.Angles();
        eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
        eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
        eyeAngles.roll = 0f;
        eyeAngles.pitch = eyeAngles.pitch.Clamp(-89.9f, 89.9f);
        Head.Transform.Rotation = Rotation.From(eyeAngles);
    }

    private void UpdateCurrentOffset()
    {
        var targetOffset = Player.IsCrouching ? Vector3.Down * 32f : Vector3.Zero;
        CurrentOffset = Vector3.Lerp(CurrentOffset, targetOffset, Time.Delta * 10f);
    }

    private void UpdateCameraPosition()
    {
        if (_camera is null) return;

        var camPos = Head.Transform.Position + CurrentOffset;
        if (!IsFirstPerson)
        {
            camPos = GetThirdPersonCameraPosition(camPos);
            _bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
        }
        else
        {
            _bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
        }

        _camera.Transform.Position = camPos;
        _camera.Transform.Rotation = Head.Transform.Rotation;
    }

    private Vector3 GetThirdPersonCameraPosition(Vector3 camPos)
    {
        var camForward = Head.Transform.Rotation.Forward;
        var camTrace = Scene.Trace.Ray(camPos, camPos - (camForward * Distance)).WithoutTags("player", "trigger").Run();

        return camTrace.Hit ? camTrace.HitPosition + camTrace.Normal : camTrace.EndPosition;
    }
}
