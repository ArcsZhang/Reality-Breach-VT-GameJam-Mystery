// using UnityEngine;
// using UnityEngine.InputSystem;

// public class PlayerControl : MonoBehaviour
// {
//     [Header("Movement")]
//     [SerializeField] private float moveSpeed = 60f;
//     [SerializeField] private float addedSpeed = 60f;

//     [Header("Jump & Gravity")]
//     [SerializeField] private float jumpHeight = 2f;
//     [SerializeField] private float gravityConst = 7f;
//     [SerializeField] private Vector2 gravity = new Vector2(0f, 0f);
//     [SerializeField] private Vector2 moveDir;
//     [SerializeField] private float coyoteTime = 0.12f;
//     [SerializeField] private float jumpBufferTime = 0.12f;
//     [SerializeField] private LayerMask groundLayer = 1 << 6;

//     [Header("Input")]
//     [SerializeField] private string moveActionName = "Move";
//     // [SerializeField] private string jumpActionName = "Jump";
//     [SerializeField] private string interactActionName = "Interact";
//     [SerializeField] private string gravityActionName = "Gravity";

//     private PlayerInput playerInput;
//     private InputAction moveAction;
//     private InputAction jumpAction;
//     private InputAction interactAction;
//     private InputAction gravityAction;
//     private Rigidbody2D rigidBody2D;
//     private float moveInputX;
//     private float moveInputY;
//     private float verticalVelocity;
//     private float coyoteTimeCounter;
//     private float jumpBufferCounter;
//     private bool isGrounded;
//     private int gravityDirection = 0;

//     private void Awake()
//     {
//         playerInput = GetComponent<PlayerInput>();
//         rigidBody2D = GetComponent<Rigidbody2D>();
//         rigidBody2D.gravityScale = 0f;

//         moveAction = playerInput.actions[moveActionName];
//         // jumpAction = playerInput.actions[jumpActionName];
//         interactAction = playerInput.actions[interactActionName];
//         gravityAction = playerInput.actions[gravityActionName];

//         if (moveAction == null)
//         {
//             Debug.LogError($"Move action not found: {moveActionName}", this);
//         }
//         // if (jumpAction == null)
//         // {
//         //     Debug.LogError($"Jump action not found: {jumpActionName}", this);
//         // }
//         if (interactAction == null)
//         {
//             Debug.LogError($"Interact action not found: {interactActionName}", this);
//         }
//         if (gravityAction == null)
//         {
//             Debug.LogError($"Gravity action not found: {gravityActionName}", this);
//         }
//     }

//     private void OnEnable()
//     {
//         moveAction?.Enable();
//         jumpAction?.Enable();
//         interactAction?.Enable();
//         gravityAction?.Enable();
//     }

//     private void OnDisable()
//     {
//         moveAction?.Disable();
//         jumpAction?.Disable();
//         interactAction?.Disable();
//         gravityAction?.Disable();
//     }

//     private void Update()
//     {
//         if (moveAction == null || interactAction == null || gravityAction == null)
//         {
//             return;
//         }

//         Vector2 inputVector = moveAction.ReadValue<Vector2>(); // Reads input coords
//         moveInputX = inputVector.x;
//         moveInputY = inputVector.y;

//         if(inputVector.magnitude > 0)
//         {
//             Debug.Log($"Movement Pressed! X: {moveInputX}, Y: {moveInputY}");
//         }
//         //isGrounded = CheckGrounded();
//         UpdateGravityDirection();
//         //checkFell();

//         //if(isGravityInverted)
//         //    gravity = Mathf.Abs(gravity);
//         //else
//         //    gravity = -Mathf.Abs(gravity);

//         if (isGrounded)
//         {
//             Debug.Log("Grounded");
//             coyoteTimeCounter = coyoteTime;
//             //if (verticalVelocity < 0f && !isGravityInverted)
//             //{
//             //    verticalVelocity = -2f;
//             //}
//             //else if (verticalVelocity > 0f && isGravityInverted)
//             //{
//             //    verticalVelocity = 2f;
//             //}
//         }
//         else
//         {
//             coyoteTimeCounter -= Time.deltaTime;
//         }

//         // if (jumpAction.WasPressedThisFrame())
//         // {
//         //     jumpBufferCounter = jumpBufferTime;
//         // }
//         // else
//         // {
//         //     jumpBufferCounter -= Time.deltaTime;
//         // }

//         if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
//         {
//             //if (isGravityInverted)
//             //    verticalVelocity = -Mathf.Abs(Mathf.Sqrt(jumpHeight * -2f * -gravity));
//             //else
//             //    verticalVelocity = Mathf.Abs(Mathf.Sqrt(jumpHeight * -2f * gravity));

//             //verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
//             jumpBufferCounter = 0f;
//             coyoteTimeCounter = 0f;
//         }

//         //verticalVelocity += gravity * Time.deltaTime;
//     }

//     private void FixedUpdate()
//     {
//         Vector2 input = new Vector2(moveInputX, moveInputY).normalized;
//         if (input.magnitude > 0.1f)
//         {
//             moveDir = input;
//         }

//         Vector2 velocity = moveDir * moveSpeed;
//         velocity = rigidBody2D.linearVelocity;
//         // if ((velocity.x*velocity.x + velocity.y*velocity.y) < (gravityConst + 10f))
//         // {
//             velocity.y += moveInputY * moveSpeed;
//             velocity.x += moveInputX * moveSpeed;
//         // }
//         rigidBody2D.linearVelocity = velocity + gravity * Time.fixedDeltaTime;
//     }

//     //private bool CheckGrounded()
//     //{
//     //    Bounds bounds = groundCheckTrigger.bounds;
//     //    Bounds bounds2 = groundCheckTrigger2.bounds;
//     //    Vector2 center = bounds.center;
//     //    Vector2 size = bounds.size;
//     //    Vector2 center2 = bounds2.center;
//     //    Vector2 size2 = bounds2.size;
//     //    return (Physics2D.OverlapBox(center, size, 0f, groundLayer) != null && !isGravityInverted) || (Physics2D.OverlapBox(center2, size2, 0f, groundLayer) != null && isGravityInverted);
//     //}

//     private void UpdateGravityDirection()
//     {
//         if (gravityAction.WasPressedThisFrame())
//         {
//             Vector2 dir = gravityAction.ReadValue<Vector2>();

//             // Up Arrow
//             if (dir.y > 0.5f) 
//             {
//                 if (gravity.x == 0f && gravity.y == gravityConst)
//                     gravity = Vector2.zero;
//                 else
//                     gravity = new Vector2(0f, gravityConst);
//             }
//             // Down Arrow
//             else if (dir.y < -0.5f) 
//             {
//                 if (gravity.x == 0f && gravity.y == -gravityConst)
//                     gravity = Vector2.zero;
//                 else
//                     gravity = new Vector2(0f, -gravityConst);
//             }
//             // Left Arrow
//             else if (dir.x < -0.5f) 
//             {
//                 if (gravity.x == -gravityConst && gravity.y == 0f)
//                     gravity = Vector2.zero;
//                 else
//                     gravity = new Vector2(-gravityConst, 0f);
//             }
//             // Right Arrow
//             else if (dir.x > 0.5f) 
//             {
//                 if (gravity.x == gravityConst && gravity.y == 0f)
//                     gravity = Vector2.zero;
//                 else
//                     gravity = new Vector2(gravityConst, 0f);
//             }
//         }
//     }

//     //private void checkFell()
//     //{
//     //    if (transform.position.y < -17f)
//     //    {
//     //        transform.position = new Vector3(transform.position.x, 17f, transform.position.z);
//     //        rigidBody2D.linearVelocity = Vector2.zero;
//     //    }
//     //    else if (transform.position.y > 17f)
//     //    {
//     //        transform.position = new Vector3(transform.position.x, -17f, transform.position.z);
//     //        rigidBody2D.linearVelocity = Vector2.zero;
//     //    }
//     //}

//     //private void OnDrawGizmosSelected()
//     //{
//     //    if (groundCheckTrigger == null)
//     //    {
//     //        return;
//     //    }

//     //    Gizmos.color = Color.yellow;
//     //    Bounds bounds = groundCheckTrigger.bounds;
//     //    Gizmos.DrawWireCube(bounds.center, bounds.size);
//     //}

	
// }
