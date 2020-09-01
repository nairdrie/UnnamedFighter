using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageController : MonoBehaviour
{
    public GameObject ledgePrefab;

    private GameObject leftLedge;
    private GameObject rightLedge;

    private PolygonCollider2D collider;


    void Awake() {
        this.collider = GetComponent<PolygonCollider2D>();
    }

    void Start()
    {
        this.leftLedge = Instantiate(this.ledgePrefab, new Vector2(-this.collider.bounds.size.x/2, transform.position.y + this.collider.bounds.size.y/2), Quaternion.identity);
        this.leftLedge.transform.parent = transform;

        this.rightLedge = Instantiate(this.ledgePrefab, new Vector2(this.collider.bounds.size.x/2, transform.position.y + this.collider.bounds.size.y/2), Quaternion.identity);
        this.rightLedge.transform.parent = transform;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
