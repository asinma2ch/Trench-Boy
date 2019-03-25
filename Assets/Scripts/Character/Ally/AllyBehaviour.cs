﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AllyBehaviour : MonoBehaviour
{
    public enum State { Shooting, Waiting, Downed, Healing };

    [Header("Personal Data")]
    [SerializeField] private Ally Identity;

    [Range(0.5f, 2)] [SerializeField] private float ShotDelay;
    [Range(0, 1)] [SerializeField] private float Accuracy;
    //[Range(0, 10)] [SerializeField] private float AimingPatience;
    //[Range(0, 1)] [SerializeField] private float SteadyHand;
    //[Range(0, 1)] [SerializeField] private float GunQuality;
    //[Range(0, 1)] [SerializeField] private float AimingPrioritizer;
    //[Tooltip("Time before this unit get back to shooting after medical request is ignnored")]
    [Range(0, 30)] [SerializeField] private float WaitingPatience;

    [Header("General Setting")]
    const float BulletLaunchForce = 4;
    [SerializeField] private int MaxAmmo = 30;
    [SerializeField] private int AmmoCount = 10;
    [SerializeField] private float BleedingEndurance = 30;
    [SerializeField] private float RecoverTime = 10;

    [SerializeField] private float MaxTargetDistance = 15;
    [SerializeField] private float UrgentTargetZDistance = 3;
    [SerializeField] private LayerMask EnemyLayer;

    [SerializeField] private Transform Muzzle;
    [SerializeField] private int BulletPoolSize = 2;
    [SerializeField] private GameObject BulletPrefab;

    //background process variable
    public State CurrentState = State.Shooting;
    private Transform CurrentTarget;
    //private float TargetDistance;
    private bool Shooting = false;
    private GameObject[] BulletPool;
    [SerializeField] private bool Injured = false;
    private bool Downed = false;
    private bool Healing = false;
    private float BleedingTimer;
    private float RecoveryTimer;

    private float WaitTimer = 0;

    [Header("Feedback and Animation")]
    [SerializeField] private Sprite AmmoRequest;
    [SerializeField] private Sprite HealRequest;
    [SerializeField] private Image RequestImage;
    [SerializeField] private Image RescueGauge;
    [SerializeField] private Image HealGauge;

    //[SerializeField] private Material FiringMaterial;
    //[SerializeField] private Material WaitingMaterial;
    //private Renderer renderer;
    private Animator animator;

    public AudioClip callMedClip;
    public AudioClip callAmmoClip;
    public AudioSource callSource;
    private bool call = false;

    private void Start()
    {
        animator = this.GetComponentInChildren<Animator>();
        //renderer = this.GetComponentInChildren<MeshRenderer>();
        BulletPool = new GameObject[BulletPoolSize];
        for (int i = 0; i < BulletPoolSize; i++)
        {
            BulletPool[i] = Instantiate(BulletPrefab);
            BulletPool[i].SetActive(false);
        }

        BleedingTimer = 1;
        RecoveryTimer = 0;
    }

    private void FixedUpdate()
    {
        //if not shooting, find new target

        if (!Downed)
        {
            if (AmmoCount > 0 && (!Injured || WaitTimer >= WaitingPatience))
            {
                CurrentState = State.Shooting;
            }
            else if (AmmoCount <= 0 || (Injured && WaitTimer < WaitingPatience))
            {
                CurrentState = State.Waiting;
            }
        }
        else if (Downed && BleedingTimer <= 0)
        {
            Dead();
        }
        else if (CurrentState != State.Healing)
        {
            CurrentState = State.Downed;
        }

        //check availability
        //if (AmmoCount > 0 && (!Injured || WaitTimer >= WaitingPatience))
        //{
        //    RequestImage.enabled = false;
        //    renderer.material = FiringMaterial;

        //    //popup
        //    this.transform.position = new Vector3(this.transform.position.x, 1, this.transform.position.z);
        //}
        //else
        //{
        //    RequestImage.enabled = true;
        //    renderer.material = WaitingMaterial;

        //    //duck
        //    this.transform.position = new Vector3(this.transform.position.x, 0.5f, this.transform.position.z);
        //}

        //set request image
        //if (Downed)
        //{
        //    //Show timer
        //}
        //else if (Injured)
        //{
        //    RequestImage.sprite = HealRequest;
        //}
        //else if (AmmoCount <= 0)
        //{
        //    RequestImage.sprite = AmmoRequest;
        //}

        switch (CurrentState)
        {
            case State.Shooting:
                //toggle UI
                RescueGauge.gameObject.SetActive(false);
                RequestImage.enabled = false;
                HealGauge.gameObject.SetActive(false);

                //renderer.material = FiringMaterial;

                if (!Shooting && AmmoCount > 0)
                {
                    StartCoroutine(Shoot());
                }
                //popup
                this.transform.position = new Vector3(this.transform.position.x, 1, this.transform.position.z);
                break;

            case State.Waiting:
                //toggle UI
                RescueGauge.gameObject.SetActive(false);
                RequestImage.enabled = true;
                HealGauge.gameObject.SetActive(false);

                //renderer.material = WaitingMaterial;


                //duck
                this.transform.position = new Vector3(this.transform.position.x, 0.5f, this.transform.position.z);

                //stop shooting
                StopCoroutine(Shoot());

                if (Injured)
                {

                    RequestImage.sprite = HealRequest;
                    if (!call)
                    {
                        callSource.clip = callMedClip;
                        callSource.Play();
                        call = true;
                    }

                    WaitTimer += Time.fixedDeltaTime;
                }
                else if (AmmoCount <= 0)
                {
                    RequestImage.sprite = AmmoRequest;

                    if (!call)
                    {
                        callSource.clip = callAmmoClip;
                        callSource.Play();
                        call = true;
                    }
                }

                break;

            case State.Downed:
                //toggle UI
                RequestImage.enabled = false;
                RescueGauge.gameObject.SetActive(true);
                HealGauge.gameObject.SetActive(false);

                //renderer.material = WaitingMaterial;

                //lie down
                this.transform.position = new Vector3(this.transform.position.x, 0.5f, this.transform.position.z);

                //stop shooting
                StopCoroutine(Shoot());

                //update timer
                BleedingTimer -= Time.fixedDeltaTime / BleedingEndurance;
                RescueGauge.transform.GetChild(0).GetComponent<Image>().fillAmount = BleedingTimer;
                break;

            case State.Healing:
                //toggle UI
                RequestImage.enabled = false;
                RescueGauge.gameObject.SetActive(false);
                HealGauge.gameObject.SetActive(true);

                if (!Healing)
                {
                    StartCoroutine(Recover());
                }

                //show timer
                RecoveryTimer += Time.fixedDeltaTime / RecoverTime;
                HealGauge.transform.GetChild(0).GetComponent<Image>().fillAmount = RecoveryTimer;

                break;
        }

        animator.SetBool("Downed", CurrentState == State.Downed);
        animator.SetBool("Calling", CurrentState == State.Waiting);
    }

    private void FindNewTarget()
    {
        //resets target
        CurrentTarget = null;

        //collect targets
        Collider[] Enemies = Physics.OverlapSphere(this.transform.position, MaxTargetDistance, EnemyLayer);

        //find target
        float ClosestPriorDistance = MaxTargetDistance;
        float ClosestLowPriorDistance = MaxTargetDistance;
        bool HasUrgentTarget = false; //target that is too close to trench line

        foreach (Collider enemy in Enemies)
        {
            float ZDistance = enemy.transform.position.z - this.transform.position.z;
            float Distance = (enemy.transform.position - this.transform.position).magnitude;

            if (ZDistance <= UrgentTargetZDistance)
            {
                HasUrgentTarget = true;
                if (Distance <= ClosestPriorDistance)
                {
                    ClosestPriorDistance = Distance;
                    CurrentTarget = enemy.transform;
                }

            }
            else if (!HasUrgentTarget)
            {
                if (Distance <= ClosestLowPriorDistance)
                {
                    ClosestLowPriorDistance = Distance;
                    CurrentTarget = enemy.transform;
                }
            }
        }
    }

    private IEnumerator Shoot()
    {
        Shooting = true;
        FindNewTarget();

        if (CurrentTarget != null)
        {
            //calculate
            //float distance = (CurrentTarget.position - this.transform.position).magnitude;
            Vector3 BulletForce = (CurrentTarget.transform.position - this.transform.position).normalized + new Vector3(Mathf.Tan(Mathf.Deg2Rad * (Random.Range(0, ((1 - Accuracy) * 5)))), Mathf.Tan(Mathf.Deg2Rad * Random.Range(0, ((1 - Accuracy) * 5))), 0);
            BulletForce.Normalize();
            BulletForce *= BulletLaunchForce;
            //aim
            yield return new WaitForSeconds(ShotDelay);
            //fire
            ShootBullet(BulletForce);
            animator.SetTrigger("Fire");
        }
        else
        {
            yield return null;
        }
        Shooting = false;
    }

    private void ShootBullet(Vector3 FiringDirection)
    {
        foreach(GameObject bullet in BulletPool)
        {
            if (!bullet.activeSelf)
            {
                bullet.SetActive(true);
                bullet.transform.position = Muzzle.position;
                bullet.GetComponent<Rigidbody>().AddForce(FiringDirection, ForceMode.Impulse);
                bullet.GetComponent<BulletBehaviour>().TargetLayer = EnemyLayer;
                AmmoCount--;
                break;
            }
        }
    }

    public void Shot()
    {
        if (!Injured)
        {
            Injured = true;
            WaitTimer = 0;
        }
        else
        {
            Down();
            RecoveryTimer = 0;
        }
    }

    private IEnumerator Recover()
    {
        Healing = true;
        yield return new WaitForSeconds(RecoverTime);
        Downed = false;
        Injured = false;

        BleedingTimer = 1;
        Healing = false;
    }

    private void Down()
    {
        Downed = true;
    }

    public bool HandItem(ItemType item)
    {
        switch (item)
        {
            case ItemType.Ammo:
                if (AmmoCount < (MaxAmmo - (int)ItemType.Ammo))
                {
                    AmmoCount += (int)ItemType.Ammo;
                    call = false;
                    return true;
                }
                else
                {
                    print("Me pouches are too heavy mate.");
                    call = false;
                    return false;
                }
            case ItemType.Med:
                if (Injured)
                {
                    Injured = false;
                    call = false;
                    WaitTimer = 0;
                    return true;
                }
                else
                {
                    print("Na, I'm good.");
                    call = false;
                    return false;
                }
        }
        Debug.Log("Wrong Item");
        return false;
    }

    private void Dead ()
    {
        this.transform.parent = null;

        //Collect deceased data
        GameController.AlliesAliveStatus[(int)Identity] = false;

        //subject to change
        this.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(this.transform.position, MaxTargetDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(this.transform.position + Vector3.left * 3 + Vector3.forward * UrgentTargetZDistance + Vector3.up * 2, this.transform.position + Vector3.right * 3 + Vector3.forward * UrgentTargetZDistance + Vector3.up * 2);
    }
}
