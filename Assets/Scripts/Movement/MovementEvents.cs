namespace Game.Movement
{
    /// <summary>Published by PlayerMovement when a walk/run step cycle completes.</summary>
    public readonly struct Footstep { }

    /// <summary>Published by PlayerMovement when the player leaves the ground.</summary>
    public readonly struct Jump { }

    /// <summary>Published by PlayerMovement when the player returns to the ground.</summary>
    public readonly struct Land { }
}
