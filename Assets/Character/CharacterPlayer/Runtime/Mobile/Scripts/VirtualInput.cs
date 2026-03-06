using FlashFriends;
using UnityEngine;

public class VirtualInput : MonoBehaviour
{
    [Header("Output")]
    public PlayerInputHandler PlayerInputHandler;

    public void VirtualMoveInput(Vector2 virtualMoveDirection)
    {
        PlayerInputHandler.MoveInput(virtualMoveDirection);
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        PlayerInputHandler.LookInput(virtualLookDirection);
    }

    public void VirtualJumpInput(bool virtualJumpState)
    {
        PlayerInputHandler.JumpInput(virtualJumpState);
    }

    public void VirtualSprintInput(bool virtualSprintState)
    {
        PlayerInputHandler.SprintInput(virtualSprintState);
    }
}
