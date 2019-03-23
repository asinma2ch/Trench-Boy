﻿using UnityEngine;

public class TrenchBoyController : MonoBehaviour
{

    private InventorySystem Inventory;

    // ------------------------------
    // game objects
    // ------------------------------
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject med_pouch;
    [SerializeField] private GameObject ammo_pouch;
    private GameObject pouch;

    // ------------------------------
    // character movement
    // ------------------------------
    public bool isMovable = true;
    public bool isCarrying = false;
    public float movementSpeed = 0.0f;
    private float maxSpeed = 0.0f;
    public float carrySpeed = 0.0f;
    public float defaultSpeed = 0.0f;
    private bool facingRight = true;
    private bool animatorMoving = false;

    // ------------------------------
    // transform position
    // ------------------------------
    public Transform Carrier;
    private Transform CarriedObject;
    [SerializeField] private Transform world;

    // ------------------------------
    // Interactable Item
    // ------------------------------
    private float carryDelay = 0.8f;
    //private bool cooldown = false;              //timer betwween put down crate and pick up pouch
    public bool carryingCrate = false;
    public float CratePickUpTime = 0.5f;
    [SerializeField] private float ItemPickUpTime = 0.1f;
    public SupplyZone ReSupplyZone = null;
    public MedicalBed MedBed = null;
    //--------------------------
    //Input control
    //--------------------------
    public bool spacebarUpped = false;             //some situation we don't want the game to register spacebar up twice 
                                                   //(such as when drop/pick up crate) so it won't pick up item at the same time


    Animator animator;
    Rigidbody rb;
    ColliderChercker Checker;
    //Crate crate; // waiting for Crate script

    Vector3 refVector = Vector3.zero;
    Vector3 facing = Vector3.zero;
    private Vector3 carryPos = new Vector3(0.0f, 0.5f, 0.0f);

    // for checking time interaction
    public float delta = 0.0f; // button hold timer
    float delay = 0.0f; // delay between interactions

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Checker = this.GetComponentInChildren<ColliderChercker>();
        Inventory = this.GetComponent<InventorySystem>();
        animator = this.GetComponentInChildren<Animator>();
        //   crate = GetComponent<Crate>(); // wating for Crate script
        facingRight = true;
    }

    private void Update()
    {
        //check if movable
        if (isMovable == true)
        {
            Move();
        }

        //other player input
        Interaction();

        //set animator parameter
        animator.SetBool("Moving", animatorMoving);
        if (Carrier.childCount > 0)
        {
            CarriedObject = Carrier.GetChild(0);

            if (CarriedObject.CompareTag("Ally"))
            {
                animator.SetInteger("Item", 1);
            }
            else if (CarriedObject.CompareTag("Crate"))
            {
                switch (CarriedObject.GetComponent<Crates>().Type)
                {
                    case ItemType.Med:
                        animator.SetInteger("Item", 2);
                        break;
                    case ItemType.Ammo:
                        animator.SetInteger("Item", 3);
                        break;
                }
            }
        }
        else
        {
            animator.SetInteger("Item", 0);
        }
    }

    void FixedUpdate()
    {
        //register carrying statuses
        if (Carrier.childCount > 0)
        {
            CarriedObject = Carrier.GetChild(0);
            isCarrying = true;

            if (Carrier.GetChild(0).CompareTag("Crate"))
            {
                carryingCrate = true;
            }
            else
            {
                carryingCrate = false;
            }
        }
        else if (!Inventory.isEmpty())
        {
            isCarrying = true;
            carryingCrate = false;
        }
        else
        {
            CarriedObject = null;
            isCarrying = false;
        }
    }

    // ↑↓→← WASD
    private void Move()
    {
        if (isMovable == true)
        {
            // ------------------------------
            // ms check
            // ------------------------------
            if (isCarrying == true)
            {
                // ms lowered while carrying
                maxSpeed = carrySpeed;
            }
            else
            {
                // if !carrying ms = default
                maxSpeed = defaultSpeed;
            }

            // ------------------------------
            // buttons for character controls
            // ------------------------------
            // LEFT
            animatorMoving = false;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                rb.velocity = Vector3.SmoothDamp(rb.velocity, Vector3.left * movementSpeed, ref refVector, 0.05f, maxSpeed);

                if (facingRight)
                {
                    Flip();
                }
                facing = Vector3.left;
                animatorMoving = true;
            }
            // RIGHT
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                rb.velocity = Vector3.SmoothDamp(rb.velocity, Vector3.right * movementSpeed, ref refVector, 0.05f, maxSpeed);

                if (!facingRight)
                {
                    Flip();
                }
                facing = Vector3.right;
                animatorMoving = true;
            }
            // UP
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                rb.velocity = Vector3.SmoothDamp(rb.velocity, Vector3.forward * movementSpeed, ref refVector, 0.05f, maxSpeed);
                facing = Vector3.forward;
                animatorMoving = true;
            }
            // DOWN
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                rb.velocity = Vector3.SmoothDamp(rb.velocity, Vector3.back * movementSpeed, ref refVector, 0.05f, maxSpeed);
                facing = Vector3.back;
                animatorMoving = true;
            }
            //-------------------------------
        }
    }

    void Flip()
    {
        this.transform.localScale = new Vector3(this.transform.localScale.x * -1, this.transform.localScale.y, this.transform.localScale.z);
        facingRight = !facingRight;
    }

    private void Interaction()
    {
        // ------------------
        // cycle inventory slot
        // ------------------
        if (Input.GetKeyDown(KeyCode.E))
        {
            Inventory.CycleInventory();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Inventory.RemoveItem();
        }

        // ------------------
        // is not Carrying
        // ------------------
        if (!spacebarUpped && Input.GetKey(KeyCode.Space))
        {
            if (!isCarrying || Inventory.HasEmptySlot())
            {
                delta += Time.deltaTime; // hold timer starts after pressing the button
                if (delta >= CratePickUpTime)
                {

                    if (!isCarrying)
                    {
                        //pick up crate
                        if (Checker.ClosestTrigerrer != null && Checker.ClosestTrigerrer.CompareTag("CargoSlot"))
                        {
                            Checker.ClosestTrigerrer.GetComponent<CargoSlot>().TakeOffCargo(Carrier, Vector3.zero);
                            delta = 0; // stops timer
                            spacebarUpped = true;  //count as key up
                        }
                        else if (Checker.ClosestTrigerrer != null && Checker.ClosestTrigerrer.CompareTag("Crate"))
                        {
                            Checker.childTransfer(Carrier);
                            delta = 0; // stops timer
                            spacebarUpped = true;  //count as key up
                        }else if (Checker.ClosestTrigerrer != null && Checker.ClosestTrigerrer.CompareTag("Ally") && Checker.ClosestTrigerrer.GetComponent<AllyBehaviour>().CurrentState == AllyBehaviour.State.Downed)
                        {
                            Checker.childTransfer(Carrier);
                            delta = 0; // stops timer
                            spacebarUpped = true;  //count as key up

                            //prevent from stuck in ground, will change later
                            Checker.ClosestTrigerrer.GetComponent<Collider>().isTrigger = true;
                        }
                        else if (ReSupplyZone != null)
                        {
                            ReSupplyZone.SpawnNew(Carrier);
                        }
                    }
                }
            }
        }

        if (!spacebarUpped && Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log("UP");
            if ((!isCarrying || Inventory.HasEmptySlot()) && delta < ItemPickUpTime)
            {
                // pick up the POUCH
                if (Checker.ClosestTrigerrer != null && Checker.ClosestTrigerrer.CompareTag("Crate"))
                {
                    Crates crate = Checker.ClosestTrigerrer.GetComponent<Crates>();

                    if (crate.Amount > 0)
                    {
                        if (Inventory.Add(crate.Type))
                        {
                            crate.Amount--;
                            isCarrying = true;
                        }
                    }
                }
            }
            delta = 0; // stops timer 
            spacebarUpped = true;
        }

        if (spacebarUpped && Input.GetKeyDown(KeyCode.Space))
        {
            spacebarUpped = false;
            // ------------------
            // is Carrying
            // ------------------
            if (isCarrying)
            {
                // POUCH
                if (!Inventory.isEmpty())
                {
                    if (Checker.ClosestTrigerrer != null && Checker.ClosestTrigerrer.gameObject.layer == 9/*Ally*/)
                    {
                        AllyBehaviour ally = Checker.ClosestTrigerrer.GetComponent<AllyBehaviour>();

                        if (ally.HandItem(Inventory.ItemInventory[Inventory.SelectedItem]))
                        {
                            Inventory.RemoveItem();
                        }
                    }
                }
                //CRATE
                else if (carryingCrate)
                {
                    if (Checker.ClosestTrigerrer != null && Checker.ClosestTrigerrer.CompareTag("CargoSlot"))
                    {
                        Checker.ClosestTrigerrer.GetComponent<CargoSlot>().StoreCargo(CarriedObject);
                        carryingCrate = false;
                        //isCarrying = false;
                    }
                    else if (ReSupplyZone != null)
                    {
                        //refill crate
                        if (CarriedObject != null && CarriedObject.GetComponent<Crates>())
                        {
                            ReSupplyZone.RefillCrate(CarriedObject.GetComponent<Crates>());
                        }
                    }
                    else
                    {
                        //put crate box
                        // ************************* will change after engine proof *****************************
                        CarriedObject.localPosition = facing;
                        CarriedObject.SetParent(world);
                        CarriedObject.position = new Vector3(CarriedObject.position.x, CarriedObject.transform.lossyScale.y * 0.5f, CarriedObject.position.z);

                        carryingCrate = false;
                        //isCarrying = false;
                    }
                    spacebarUpped = true;
                    delta = 0;
                }
                //ALLY
                else if (CarriedObject.CompareTag("Ally"))
                {
                    if (MedBed != null)
                    {
                        //reenable collider, will change later
                        CarriedObject.GetComponent<Collider>().isTrigger = false;

                        MedBed.PutPatient(CarriedObject);
                    }
                    else
                    {
                        //just put down
                        CarriedObject.localPosition = facing;
                        CarriedObject.SetParent(world);
                        CarriedObject.position = new Vector3(CarriedObject.position.x, CarriedObject.transform.lossyScale.y * 0.5f, CarriedObject.position.z);

                        //reenable collider, will change later
                        CarriedObject.GetComponent<Collider>().isTrigger = false;

                        carryingCrate = false;
                    }
                }

                //set bool in animator
                animator.SetInteger("Item", 0);
            }
        }

    }

}
