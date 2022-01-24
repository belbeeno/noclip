using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotionDriver : StateMachineBehaviour
{
    public HeroMovement owner = null;

    int tDeadIndex = -1;
    int fForwardIndex = -1;
    int fRightIndex = -1;
    int bSneakIndex = -1;
    int tStumbleIndex = -1;
    bool Initialized
    {
        get => fForwardIndex >= 0 && fRightIndex >= 0 && bSneakIndex >= 0 && tStumbleIndex >= 0;
    }
    private bool died = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Initialized) return;
        owner = animator.transform.parent.GetComponent<HeroMovement>();
        for (int i = 0; i < animator.parameterCount; ++i)
        {
            string name = animator.parameters[i].name;
            if (name.Equals("MovementFwd"))
            {
                fForwardIndex = animator.parameters[i].nameHash;
            }
            else if (name.Equals("MovementRight"))
            {
                fRightIndex = animator.parameters[i].nameHash;
            }
            else if (name.Equals("Sneak"))
            {
                bSneakIndex = animator.parameters[i].nameHash;
            }
            else if (name.Equals("Stumble"))
            {
                tStumbleIndex = animator.parameters[i].nameHash;
            }
            else if (name.Equals("Dead"))
            {
                tDeadIndex = animator.parameters[i].nameHash;
            }
            else
            {
                Debug.LogError("LocomotionDriver does not know how to handle parameter " + name + "; intentional?");
            }
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (owner == null || died) return;
        animator.SetFloat(fRightIndex, owner.VelocityNormalized.x);
        animator.SetFloat(fForwardIndex, owner.VelocityNormalized.z);
        if (owner.SpiritJustTransferred)
        {
            animator.SetTrigger(tStumbleIndex);
        }
        if (owner.IsDead)
        {
            animator.SetTrigger(tDeadIndex);
            died = true;
        }
    }
}
