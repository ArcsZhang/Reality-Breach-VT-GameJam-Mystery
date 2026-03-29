using UnityEngine;
using UnityEngine.InputSystem;

public class GravityAbility : Ability
{
	[SerializeField] private ObjectBehavior playerObject;
	
	[Header("Gravity")]
	[SerializeField] private float gravityStrength = 9.81f;
	[SerializeField] private float centerDeadzonePixels = 80f;
	[SerializeField] private Vector2 startingGravity = Vector2.zero;

	[Header("Indicator")]
	[SerializeField] private float indicatorLengthWorld = 2.5f;
	[SerializeField] private float indicatorWidth = 0.06f;
	[SerializeField] private int deadzoneRingSegments = 32;
	[SerializeField] private Color indicatorColor = new Color(0.4f, 0.9f, 1f, 0.95f);
	[SerializeField] private Color neutralColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
	[SerializeField] private Color deadzoneRingColor = new Color(1f, 0.95f, 0.2f, 0.7f);

    [SerializeField] private float cooldownDuration = 3f; // different per ability
    private float cooldownTimer = 0f;
    public bool IsOnCooldown => cooldownTimer > 0f;
    public float GetCooldownProgress() => Mathf.Clamp01(cooldownTimer / cooldownDuration);

    private LineRenderer directionLine;
	private LineRenderer deadzoneRing;

	private void Awake()
	{
		if (WorldCamera == null)
		{
			WorldCamera = Camera.main;
		}

		InitializeVisuals();
		SetVisualsEnabled(false);
		Physics2D.gravity = startingGravity;
	}

	public override void Initialize(Camera cam)
	{
		base.Initialize(cam);
		if (WorldCamera == null)
		{
			WorldCamera = Camera.main;
		}
	}
    private void TriggerCooldown()
    {
        cooldownTimer = cooldownDuration;
    }

    public override void onUpdate()
	{
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (WorldCamera == null)
		{
			WorldCamera = Camera.main;
		}

		Mouse mouse = Mouse.current;
		if (mouse == null || WorldCamera == null)
		{
			return;
		}

		Vector2 mouseScreen = mouse.position.ReadValue();
		Vector2 direction = GetScreenDirection(mouseScreen, out bool isNeutral);
		bool canChangeGravity = CanChangeGravityNow();

		UpdateVisualIndicator(direction, isNeutral, canChangeGravity);
		SetRingEnabled(true);
		SetDirectionLineEnabled(canChangeGravity);

		if (!canChangeGravity)
		{
			return;
		}

		if (mouse.leftButton.wasPressedThisFrame)
		{
			TriggerCooldown();
			Debug.Log($"Gravity direction set to {direction} (neutral: {isNeutral})");
			if (isNeutral)
			{
				Physics2D.gravity = Vector2.zero;
			}
			else
			{
				Physics2D.gravity = direction * gravityStrength;
			}
		}
	}

	public override void onClear()
	{
		SetVisualsEnabled(false);
	}

	public override void onAbilitySwitch()
	{
		SetVisualsEnabled(false);
	}

	private Vector2 GetScreenDirection(Vector2 mouseScreen, out bool isNeutral)
	{
		Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
		Vector2 delta = mouseScreen - center;

		if (delta.magnitude <= centerDeadzonePixels)
		{
			isNeutral = true;
			return Vector2.zero;
		}

		isNeutral = false;

		if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
		{
			return delta.x >= 0f ? Vector2.right : Vector2.left;
		}

		return delta.y >= 0f ? Vector2.up : Vector2.down;
	}

	private void UpdateVisualIndicator(Vector2 direction, bool isNeutral, bool canChangeGravity)
	{
		Vector3 centerWorld = ScreenToWorld(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
		float ringRadiusWorld = Mathf.Max(0.001f, PixelsToWorldDistance(centerDeadzonePixels));

		UpdateDeadzoneRing(centerWorld, ringRadiusWorld);

		if (!canChangeGravity)
		{
			return;
		}

		if (isNeutral)
		{
			directionLine.startColor = neutralColor;
			directionLine.endColor = neutralColor;
			directionLine.SetPosition(0, centerWorld + Vector3.left * ringRadiusWorld * 0.6f);
			directionLine.SetPosition(1, centerWorld + Vector3.right * ringRadiusWorld * 0.6f);
			return;
		}

		directionLine.startColor = indicatorColor;
		directionLine.endColor = indicatorColor;

		Vector3 dir3 = new Vector3(direction.x, direction.y, 0f);
		directionLine.SetPosition(0, centerWorld);
		directionLine.SetPosition(1, centerWorld + dir3 * indicatorLengthWorld);
	}

	private void InitializeVisuals()
	{
		Material lineMaterial = CreateLineMaterial();

		GameObject directionObj = new GameObject("GravityDirectionIndicator");
		directionObj.transform.SetParent(transform, false);
		directionLine = directionObj.AddComponent<LineRenderer>();
		directionLine.useWorldSpace = true;
		directionLine.positionCount = 2;
		directionLine.startWidth = indicatorWidth;
		directionLine.endWidth = indicatorWidth;
		directionLine.numCapVertices = 4;
		directionLine.material = lineMaterial;
		directionLine.sortingOrder = 300;

		GameObject ringObj = new GameObject("GravityDeadzoneRing");
		ringObj.transform.SetParent(transform, false);
		deadzoneRing = ringObj.AddComponent<LineRenderer>();
		deadzoneRing.useWorldSpace = true;
		deadzoneRing.loop = true;
		deadzoneRing.positionCount = Mathf.Max(8, deadzoneRingSegments);
		deadzoneRing.startWidth = indicatorWidth * 0.6f;
		deadzoneRing.endWidth = indicatorWidth * 0.6f;
		deadzoneRing.material = lineMaterial;
		deadzoneRing.startColor = deadzoneRingColor;
		deadzoneRing.endColor = deadzoneRingColor;
		deadzoneRing.sortingOrder = 300;
	}

	private void UpdateDeadzoneRing(Vector3 centerWorld, float radius)
	{
		int count = deadzoneRing.positionCount;
		for (int i = 0; i < count; i++)
		{
			float t = (float)i / count;
			float angle = t * Mathf.PI * 2f;
			Vector3 p = centerWorld + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
			deadzoneRing.SetPosition(i, p);
		}
	}

	private float PixelsToWorldDistance(float pixels)
	{
		Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, GetCameraDepth());
		Vector3 edgeScreen = new Vector3(centerScreen.x + pixels, centerScreen.y, centerScreen.z);
		Vector3 centerWorld = WorldCamera.ScreenToWorldPoint(centerScreen);
		Vector3 edgeWorld = WorldCamera.ScreenToWorldPoint(edgeScreen);
		return Vector3.Distance(centerWorld, edgeWorld);
	}

	private Vector3 ScreenToWorld(Vector2 screenPoint)
	{
		return WorldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, GetCameraDepth()));
	}

	private float GetCameraDepth()
	{
		if (WorldCamera.orthographic)
		{
			return Mathf.Abs(WorldCamera.transform.position.z);
		}

		return 10f;
	}

	private void SetVisualsEnabled(bool enabled)
	{
		SetDirectionLineEnabled(enabled);
		SetRingEnabled(enabled);
	}

	private void SetDirectionLineEnabled(bool enabled)
	{
		if (directionLine != null)
		{
			directionLine.enabled = enabled;
		}
	}

	private void SetRingEnabled(bool enabled)
	{
		if (deadzoneRing != null)
		{
			deadzoneRing.enabled = enabled;
		}
	}

	private bool CanChangeGravityNow()
	{
		if (playerObject == null)
		{
			return true;
		}

		return playerObject.IsGrounded();
	}

	private static Material CreateLineMaterial()
	{
		Shader shader = Shader.Find("Sprites/Default");
		if (shader == null)
		{
			shader = Shader.Find("Unlit/Color");
		}

		return new Material(shader);
	}
}
