﻿using System.Collections;
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
    JUMPLESS,
    LEDGEGRAB
}

public class CharacterController : MonoBehaviour
{
    private bool isMortal;
    private int immortalTimer;
    private float jumpPressedTime;
    
    private PLAYER_STATE playerState;
    private PLAYER_STATE prevPlayerState;

    private float oldMovementInput;
    private float oldOldMovementInput;
    private float movementInput;
    private float dashTime;
    private bool isNearLedge;

    private GameObject stage;
    private PolygonCollider2D stageCollider;




    public float acceleration;
    public float moveSpeed;
    public float dashSpeed;
    public float dashThreshold;
    public float maxDashTime;
    public float jumpForce;
    public float airJumpForce;
    public float shortJumpForce;
    public float shortAirJumpForce;
    public float spawnHeight;
    public float fullJumpTime;
    


    // COMPONENTS
    private Rigidbody2D rigidbody;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D collider;

    void Awake() {
        this.rigidbody = GetComponent<Rigidbody2D>();
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        this.collider = GetComponent<BoxCollider2D>();

        this.immortalTimer = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        this.stage = GameObject.Find("Stage");
        this.stageCollider = this.stage.GetComponent<PolygonCollider2D>();

        Respawn();
    }

    // Update is called once per frame
    void Update()
    {
        HandleControls();

        CalculateState();

        //HandleMotion();
        
        UpdateVisuals();

    }

    void LateUpdate() {
        HandleMotion();
    }

    void HandleControls() {
        if(CrossPlatformInputManager.GetButtonDown("Jump")) {
            this.jumpPressedTime = Time.time;
            if(this.playerState == PLAYER_STATE.AIRBORNE) {
                Debug.Log("AIR JUMP");
                this.playerState = PLAYER_STATE.JUMPLESS;
            } 
            else if(this.playerState != PLAYER_STATE.JUMPLESS) {
                Debug.Log("GROUND JUMP");
                this.prevPlayerState = this.playerState;
                this.playerState = PLAYER_STATE.AIRBORNE;
            }
            else {
                Debug.Log("JUMPLESS");
            }
                
        }
        if(CrossPlatformInputManager.GetButton("Jump")) {
            if(this.jumpPressedTime + this.fullJumpTime >= Time.time) {
                if(this.playerState == PLAYER_STATE.AIRBORNE) {
                    this.rigidbody.velocity = Vector2.up * this.jumpForce;
                } 
                else if(this.playerState == PLAYER_STATE.JUMPLESS) {
                    this.rigidbody.velocity = Vector2.up * this.airJumpForce;
                }
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

        Debug.Log(this.playerState);
        //TODO: IF STATE IS SLIDE, DONT CHANGE IT UNTIL DONE SLIDING

        if(this.playerState == PLAYER_STATE.AIRBORNE 
        || this.playerState == PLAYER_STATE.JUMPLESS 
        || this.playerState == PLAYER_STATE.SPAWNED 
        || this.playerState == PLAYER_STATE.LEDGEGRAB) {
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

        //Check for ledge grab
        if(GetBottom() < GetStageTop() && this.isNearLedge && this.playerState != PLAYER_STATE.LEDGEGRAB) {
            GrabLedge();
        }




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
            case PLAYER_STATE.LEDGEGRAB:
                translation = Vector2.zero;
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

    void OnTriggerEnter2D(Collider2D col) {
        if(col.tag == "Ledge") {
            this.isNearLedge = true;
        }
    }

    void OnTriggerExit2D(Collider2D col) {
        if(col.tag == "Stage Boundary") {
            Die();
        }

        if(col.tag == "Ledge") {
            this.isNearLedge = false;
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

    void GrabLedge() {
        this.playerState = PLAYER_STATE.LEDGEGRAB;
        this.isMortal = false;
        this.immortalTimer = 0;
        this.rigidbody.bodyType = RigidbodyType2D.Kinematic;
        this.rigidbody.velocity = Vector2.zero;
        Invoke("BecomeMortal", 5);

        if(transform.position.x < 0) {
            //grab left ledge
            transform.position = new Vector3(GetStageLeft() - GetWidth()/2, GetStageTop() - GetHeight()/2, 0);

        }
        else {
            //grab right ledge
            transform.position = new Vector3(GetStageRight() + GetWidth()/2, GetStageTop() - GetHeight()/2, 0);
        }
    }

    void UpdateVisuals() {
        if(this.isMortal) {
            this.immortalTimer = 0;
            this.spriteRenderer.color = new Color(1,52/255,52/255,1);

        }
        else {
            this.spriteRenderer.color = new Color(1,52/255, 52/255, 0.25f * Mathf.Cos(immortalTimer/4) + 0.75f);
            this.immortalTimer++;
        }

    }






    float GetBottom() {
        return transform.position.y - GetHeight()/2;
    }
    float GetTop() {
        return transform.position.y + GetHeight()/2;
    }

    float GetStageTop() {
        return this.stage.transform.position.y + this.stageCollider.bounds.size.y/2;
    }

    float GetStageBottom() {
        return this.stage.transform.position.y - this.stageCollider.bounds.size.y/2;
    }

    float GetStageLeft() {
        return this.stage.transform.position.x - this.stageCollider.bounds.size.x/2;
    }

    float GetStageRight() {
        return this.stage.transform.position.x + this.stageCollider.bounds.size.x/2;
    }

    float GetWidth() {
        return this.collider.bounds.size.x;
    }

    float GetHeight() {
        return this.collider.bounds.size.y;
    }

    
}
