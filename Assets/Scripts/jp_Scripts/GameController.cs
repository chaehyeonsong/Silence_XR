using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoftKitty.LiquidContainer;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using Unity.VisualScripting;
using UnityEngine.Experimental.GlobalIllumination;

public class GameController : MonoBehaviour
{
    //game status variables
    [HideInInspector]
    public bool game_win = false;
    [HideInInspector]
    public bool fail = false;
    [HideInInspector]
    public bool speedticket = false;
    [HideInInspector]
    public bool is_game_running { get; private set; } = false;
    [HideInInspector]
    public Color target_color;
    [HideInInspector]
    public float similarity_goal;

    [HideInInspector]
    public GameObject Linetracer;
    [HideInInspector]
    public GameObject liquid_puzzle;
    [HideInInspector]
    public GameObject liquid_checker;
    [HideInInspector]
    public Communicator communicator;
    [HideInInspector]
    public Communicator_Liquid com_liquid;
    [HideInInspector]
    public Communicator_Checker com_check;

    public string Linetracer_difficulty = "normal";
    public string Liquid_difficulty = "normal";
    [Tooltip("Only change y value")]
    public Vector3 MixingFlaskOffset;
    [Tooltip("How far back Linetracer spawns from flask")]
    public float spacer = 3f;
    [Tooltip("Liquid checkbox offset")]
    public Vector3 CheckboxOffset;
    public GameObject Linetracer_Game;
    public GameObject LiquidPuzzle;
    public GameObject LiquidChecker;
    public GameObject XR_main_camera;
    [HideInInspector]
    public AudioSource audioSource;
    public InputActionReference rightPrimary;
    public InputActionReference rightSecondary;
    public InputActionReference leftPrimary;
    public InputActionReference leftSecondary;
    [HideInInspector]
    public LiquidControl MixingFlask;


    //VR codes, which I HATE, shithead
    private void OnEnable()
    {
        if (rightPrimary != null && rightPrimary.action != null)
        {
            rightPrimary.action.Enable();
            rightPrimary.action.performed += RightPrimaryPressed;
            rightPrimary.action.canceled += RightPrimaryReleased;
        }

        if (rightSecondary != null && rightSecondary.action != null)
        {
            rightSecondary.action.Enable();
            rightSecondary.action.performed += RightSecondaryPressed;
            rightSecondary.action.canceled += RightSecondaryReleased;
        }

        if (leftPrimary != null && leftPrimary.action != null)
        {
            leftPrimary.action.Enable();
            leftPrimary.action.performed += LeftPrimaryPressed;
            leftPrimary.action.canceled += LeftPrimaryReleased;
        }

        if (leftSecondary != null && leftSecondary.action != null)
        {
            leftSecondary.action.Enable();
            leftSecondary.action.performed += LeftSecondaryPressed;
            leftSecondary.action.canceled += LeftSecondaryReleased;
        }
    }

    private void OnDisable()
    {
        if (rightPrimary != null && rightPrimary.action != null)
        {
            rightPrimary.action.performed -= RightPrimaryPressed;
            rightPrimary.action.canceled -= RightPrimaryReleased;
            rightPrimary.action.Disable();
        }

        if (rightSecondary != null && rightSecondary.action != null)
        {
            rightSecondary.action.performed -= RightSecondaryPressed;
            rightSecondary.action.canceled -= RightSecondaryReleased;
            rightSecondary.action.Disable();
        }

        if (leftPrimary != null && leftPrimary.action != null)
        {
            leftPrimary.action.performed -= LeftPrimaryPressed;
            leftPrimary.action.canceled -= LeftPrimaryReleased;
            leftPrimary.action.Disable();
        }

        if (leftSecondary != null && leftSecondary.action != null)
        {
            leftSecondary.action.performed -= LeftSecondaryPressed;
            leftSecondary.action.canceled -= LeftSecondaryReleased;
            leftSecondary.action.Disable();
        }
    }

    private void RightPrimaryPressed(InputAction.CallbackContext context)
    {
        if (liquid_checker != null)
        {
            com_check.Check_answer();
        }
        else
        {
            Play_game();
        }
    }
    private void RightPrimaryReleased(InputAction.CallbackContext context)
    {
        //Do nothing
    }
    private void RightSecondaryPressed(InputAction.CallbackContext context)
    {
        Reset_game();
    }
    private void RightSecondaryReleased(InputAction.CallbackContext context)
    {
        //Do nothing
    }

    private void LeftPrimaryPressed(InputAction.CallbackContext context)
    {
        if (liquid_checker != null)
        {
            com_check.Check_answer();
        }
        else
        {
            Play_game();
        }
    }
    private void LeftPrimaryReleased(InputAction.CallbackContext context)
    {
        //Do nothing
    }
    private void LeftSecondaryPressed(InputAction.CallbackContext context)
    {
        Reset_game();
    }
    private void LeftSecondaryReleased(InputAction.CallbackContext context)
    {
        //Do nothing
    }

    public void Play_game() //activated by right controller primary button (A)
    {
        if (is_game_running == false && fail == false && game_win == false)
        {
            //instantiate the linetracer prefab at mixing flask position, facing straight up
            Vector3 flaskPos = MixingFlask.transform.position - transform.position
                               + MixingFlaskOffset;
            Vector3 direction = MixingFlask.transform.position - XR_main_camera
                                .transform.position;
            direction.y = 0f;
            direction.Normalize();
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Debug.Log(angle);
            Linetracer = Instantiate(Linetracer_Game, transform);
            Linetracer.transform.RotateAround(transform.position, Vector3.up, angle);
            //Debug.Log(Linetracer.transform.position);
            Linetracer.transform.position += flaskPos - direction * spacer;
            //Debug.Log(Linetracer.transform.position);

            Linetracer.SetActive(true);
            communicator = Linetracer.GetComponent<Communicator>();

            communicator.flask = MixingFlask;
            communicator.normal = direction;
            communicator.Prepare_game(Linetracer_difficulty);
            is_game_running = true;
            communicator.is_game_running = is_game_running;
        }
    }

    public void Reset_game() //activated by left controller primary button (A)
    {
        is_game_running = false;
        if (communicator != null)
        {
            communicator.is_game_running = is_game_running;
        }
        game_win = false;
        fail = true;
        Restarter();
    }

    public void Restarter()
    {
        audioSource.Play(); //play shatter sound, unaffected by flask positions
        Destroy(Linetracer);
        Destroy(liquid_puzzle);
        DestroyAllRemnants();
        Destroy(liquid_checker);
        GameSetup();
        fail = false;
        speedticket = false;
    }

    void DestroyAllRemnants()
    {
        foreach (var flask in FindObjectsOfType<LiquidControl>())
        {
            Destroy(flask.gameObject);
        }
        
        foreach (var spray in FindObjectsOfType<ParticleSystem>())
        {
            Destroy(spray.gameObject);
        }
    }

    public void GameSetup()//spawn flasks, liquid color puzzle, 
    {
        liquid_puzzle = Instantiate(LiquidPuzzle, transform);
        Vector3 offset = new Vector3(0f, 0f, -0.3f);
        liquid_puzzle.transform.position += offset;
        com_liquid = liquid_puzzle.GetComponent<Communicator_Liquid>();
        com_liquid.gameController = this;
        com_liquid.GenerateNewGame(Liquid_difficulty);
        MixingFlask = com_liquid.target_flask;
    }

    void Start()
    {
        audioSource = this.GetComponent<AudioSource>();
        //GameSetup();
    }

    void Update()
    {
        if (speedticket) //when any flask collided too quickly
        {
            fail = true;
            Restarter();
        }

        if (communicator != null) //when linetracer game is playing
        {
            if (communicator.is_game_running == true)//gameplay tracker
            {
                if (communicator.out_of_playground == true) //when you left playground
                {
                    is_game_running = false;
                    communicator.is_game_running = is_game_running;
                    game_win = false;
                    fail = true;
                }

                if (communicator.missed_line == true) //when unhit sphere hit playground
                {
                    is_game_running = false;
                    communicator.is_game_running = is_game_running;
                    game_win = false;
                    fail = true;
                }

                if (communicator.out_of_playground == false & communicator.missed_line == false &
                    communicator.finish_broadcast == true) //when you beat linetracer game
                {
                    is_game_running = false;
                    communicator.is_game_running = is_game_running;
                    communicator.finish_broadcast = false;
                    game_win = true;
                }
            }
            else if (fail == true) //when you fail game, restart the game
            {
                Restarter();
            }
            else //when linetracer isn't running for whatever reason, destroy game instance
            {
                Destroy(Linetracer);
            }
        }
        
        if (game_win == true)
        {
            liquid_checker = Instantiate(LiquidChecker, transform);
            liquid_checker.transform.position += CheckboxOffset;
            com_check = liquid_checker.GetComponent<Communicator_Checker>();
            com_check.gameController = this;
            com_check.target_flask = MixingFlask;
            com_liquid.NowChecking();
            game_win = false;
        }

        if (liquid_checker != null)
        {
            if (com_check.check_complete == true)
            {
                if (com_check.result == true) // when color is good
                {
                    Debug.Log("Should move to end scene");
                }
                else //when color is bad
                {
                    fail = true;
                    Restarter();
                }
            }
        }
    }
}
