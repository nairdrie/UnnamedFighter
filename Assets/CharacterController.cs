using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

enum PLAYER_STATE {
    SPAWNED,
    IDLE,
    MOVE,
    DASH,
    SLIDE,
    AIRBORNE,
    JUMPLESS
}

public class CharacterController : MonoBehaviour
{
    private bool isMortal;
    private int immortalTimer;
    private PLAYER_STATE playerState;
    private PLAYER_STATE prevPlayerState;

    private float oldMovementInput;
    private float oldOldMovementInput;
    private float movementInput;
    private float dashTime;

    public float acceleration;
    public float moveSpeed;
    public float dashSpeed;
    public float dashThreshold;
    public float maxDashTime;
    public float jumpForce;
    public float airJumpForce;
    public float spawnHeight;
    


    // COMPONENTS
    private Rigidbody2D rigidbody;
    private SpriteRenderer spriteRenderer;

    void Awake() {
        this.rigidbody = GetComponent<Rigidbody2D>();
        this.spriteRenderer = GetComponent<SpriteRenderer>();

        this.immortalTimer = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        Respawn();
    }

    // Update is called once per frame
    void Update()
    {
        HandleControls();

        CalculateState();

        HandleMotion();
        
        UpdateVisuals();

    }

    void HandleControls() {
        if(CrossPlatformInputManager.GetButtonDown("Jump")) {
            if(this.playerState == PLAYER_STATE.AIRBORNE) {
                Debug.Log("AIR JUMP");
                this.rigidbody.velocity = Vector2.up * this.airJumpForce;
                this.playerState = PLAYER_STATE.JUMPLESS;
            } 
            else if(this.playerState != PLAYER_STATE.JUMPLESS) {
                Debug.Log("GROUND JUMP");
                this.prevPlayerState = this.playerState;
                this.rigidbody.velocity = Vector2.up * this.jumpForce;
            }
            else {
                Debug.Log("JUMPLESS");
            }
                
        }

        this.oldOldMovementInput = this.oldMovementInput;
        this.oldMovementInput = this.movementInput;
        this.movementInput = CrossPlatformInputManager.GetAxis("Horizontal");

        if(this.movementInput != 0 && this.playerState == PLAYER_STATE.SPAWNED) {
            this.playerState = PLAYER_STATE.IDLE;
            BecomeMortal();
        }
    }

    void CalculateState() {

        //TODO: IF STATE IS SLIDE, DONT CHANGE IT UNTIL DONE SLIDING

        if(this.playerState == PLAYER_STATE.AIRBORNE || this.playerState == PLAYER_STATE.JUMPLESS) {
            return;
        }

        if(Mathf.Abs(this.movementInput - this.oldOldMovementInput) > this.dashThreshold && Mathf.Abs(this.movementInput) > 0.9) {
            
            if(this.playerState == PLAYER_STATE.DASH) {
                this.dashTime += Time.deltaTime;
            }
            
            
            this.playerState = PLAYER_STATE.DASH;
        }
        else if(this.playerState == PLAYER_STATE.DASH && Mathf.Abs(this.movementInput) > 0.9) {
            if(this.playerState == PLAYER_STATE.DASH) {
                this.dashTime += Time.deltaTime;
            }

            this.playerState = PLAYER_STATE.DASH;
        }
        else if(Mathf.Abs(this.movementInput - this.oldMovementInput) > 0) {
             
             if(this.dashTime > this.maxDashTime) {
                 this.playerState = PLAYER_STATE.SLIDE;
             }
             else {
                 this.playerState = PLAYER_STATE.MOVE;
             }
             
             this.dashTime = 0;
             
        }
        else if(this.movementInput == 0){
            if(this.dashTime > this.maxDashTime) {
                 this.playerState = PLAYER_STATE.SLIDE;
             }
             else {
                 this.playerState = PLAYER_STATE.IDLE;
             }
             
             this.dashTime = 0;
        }
    }

    void HandleMotion() {

        Vector2 translation;

        switch(this.playerState) {
            case PLAYER_STATE.IDLE:
            case PLAYER_STATE.MOVE:
                translation = Vector2.right * Time.deltaTime * this.movementInput * this.moveSpeed;
                break;
            case PLAYER_STATE.DASH:
                translation = Vector2.right * Time.deltaTime * this.movementInput * this.dashSpeed;
                break;
            case PLAYER_STATE.SLIDE:
                translation = Vector2.right * Time.deltaTime * /* TODO: MULTIPLY BY SOME VALUE WHICH GETS SMALLER  * */this.dashSpeed;
                break;
            case PLAYER_STATE.AIRBORNE:
            case PLAYER_STATE.JUMPLESS:
                if(this.prevPlayerState == PLAYER_STATE.DASH) {
                    translation = Vector2.right * Time.deltaTime * this.movementInput * this.dashSpeed;
                }
                else {
                    translation = Vector2.right * Time.deltaTime * this.movementInput * this.moveSpeed;
                }
                break;
            default:
                translation = Vector2.right * Time.deltaTime * this.movementInput * this.moveSpeed;
                break;
        }

        transform.Translate(translation);


        transform.eulerAngles = Vector3.zero;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.tag == "Stage") {
            this.playerState = PLAYER_STATE.IDLE;
        }
    
    }

    void OnCollisionStay2D(Collision2D col)
    {
        
    
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.collider.tag == "Stage") {
            this.playerState = PLAYER_STATE.AIRBORNE;
        }
        
    
    }

    void OnTriggerExit2D(Collider2D col) {
        if(col.tag == "Stage Boundary") {
            Die();
        }
    }

    void Die() {
        // DEATH STUFF
        Debug.Log("DIED");
        Invoke("Respawn", 1);
    }

    void Respawn() {
        
        Debug.Log("RESPAWNING");
        this.playerState = PLAYER_STATE.SPAWNED;
        this.isMortal = false;
        this.immortalTimer = 0;
        this.rigidbody.bodyType = RigidbodyType2D.Kinematic;
        this.rigidbody.velocity = Vector2.zero;
        transform.position = new Vector3(0, this.spawnHeight, 0);
        Invoke("BecomeMortal", 5);
    }

    void BecomeMortal() {
        //CANCEL ANY PREVIOUS CALLS
         CancelInvoke("BecomeMortal");

        this.isMortal = true;
        this.rigidbody.bodyType = RigidbodyType2D.Dynamic;
    }

    void UpdateVisuals() {
        if(this.isMortal) {
            this.immortalTimer = 0;
            Color tmp = this.spriteRenderer.color;
            tmp.a = 255;
            this.spriteRenderer.color = tmp;

        }
        else {

            this.spriteRenderer.color = new Color(255,52,52, 128 * (0.5f * (Mathf.Cos(immortalTimer) + 1)) + 128);
            this.immortalTimer++;
        }

    }


    
}
