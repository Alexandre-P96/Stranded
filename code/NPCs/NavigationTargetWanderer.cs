using Sandbox;
using System;

public sealed class NavigationTargetWanderer : Component
{
    // List of possible targets for the NPC to chase
    [Property] public List<GameObject> PotentialTargets { get; set; }
    // Reference to the NavMeshAgent for movement
    [RequireComponent] NavMeshAgent Agent { get; set; }
    // Reference to the model for animation updates
    [Property] public SkinnedModelRenderer Body { get; set; }
    // How far the NPC can "see" or wander
    [Property] public float WanderRadius { get; set; } = 450f;

    // The position the NPC is currently moving toward
    private Vector3 _currentTarget = Vector3.Zero;
    private bool _isWandering = false;
    
    protected override void OnEnabled()
    {
        PickWanderPoint(); // Start by picking a random wander point
    }
    
    protected override void OnFixedUpdate()
    {
        UpdateTarget(); // Decide whether to chase or wander
        Agent.MoveTo(_currentTarget); // Move toward the current target
        UpdateAnimation(); // Update animation parameters
    }

    // Decides what the NPC should target
    private void UpdateTarget()
    {
        var closest = FindClosestTargetWithinRadius(); // Look for a close target
        if (closest != null)
        {
            SetChaseTarget(closest); // If found, chase it
        }
        else
        {
            UpdateWanderTarget(); // Otherwise, wander
        }
    }

    // Finds the closest target within the wander radius
    private GameObject FindClosestTargetWithinRadius()
    {
        GameObject closest = null;
        float closestDist = float.MaxValue;

        if (PotentialTargets is { Count: > 0 })
        {
            foreach (var target in PotentialTargets)
            {
                float dist = WorldPosition.Distance(target.WorldPosition);
                if (dist < WanderRadius && dist < closestDist)
                {
                    closest = target; // Keep the closest one within radius
                    closestDist = dist;
                }
            }
        }

        return closest; // Returns null if none are close enough
    }

    // Sets the NPC to chase a specific target
    private void SetChaseTarget(GameObject target)
    {
        _currentTarget = target.WorldPosition;
        _isWandering = false; // Not wandering anymore
    }

    // Handles wandering logic
    private void UpdateWanderTarget()
    {
        // If not already wandering, or reached the wander point, pick a new one
        if (!_isWandering || WorldPosition.Distance(_currentTarget) < 32f)
        {
            PickWanderPoint();
        }
    }

    // Picks a random point within the wander radius to move to
    private void PickWanderPoint()
    {
        var angle = Random.Shared.Float(0, MathF.PI * 2);
        var distance = Random.Shared.Float(0, WanderRadius);
        var offset = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0) * distance;
        _currentTarget = WorldPosition + offset;
        _isWandering = true; // Now wandering
    }

    // Updates animation parameters based on movement
    private void UpdateAnimation()
    {
        var dir = Agent.Velocity;
        var forward = WorldRotation.Forward.Dot(dir);
        var sideward = WorldRotation.Right.Dot(dir);
        var angleDeg = MathF.Atan2(sideward, forward).RadianToDegree().NormalizeDegrees();

        Body.Set("move_direction", angleDeg);
        Body.Set("move_speed", Agent.Velocity.Length);
        Body.Set("move_groundspeed", Agent.Velocity.WithZ(0).Length);
        Body.Set("move_y", sideward);
        Body.Set("move_x", forward);
        Body.Set("move_z", Agent.Velocity.z);
    }
}
