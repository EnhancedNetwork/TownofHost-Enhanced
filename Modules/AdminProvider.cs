using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace TOHE.Modules;

public static class AdminProvider
{
    // ref: MapCountOverlay.Update
    /// <summary>
    /// Obtains admin information at the time of execution
    /// </summary>
    /// <returns>Key: Room SystemType, Value: <see cref="AdminEntry"/>and the dictionaries sorted by key</returns>
    public static SortedDictionary<SystemTypes, AdminEntry> CalculateAdmin()
    {
        SortedDictionary<SystemTypes, AdminEntry> allAdmins = [];
        // Store the PlayerId of those who have already been counted.
        // If false is returned when trying to add to it, it is not counted, so that each player is counted only once.
        HashSet<int> countedPlayers = new(15);
        // Array to be used for storing the detected hits Handled by Il2CppReferenceArray to avoid load during conversion
        Il2CppReferenceArray<Collider2D> colliders = new(45);
        // ref: MapCountOverlay.Awake
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = Constants.LivingPlayersOnlyMask,
            useTriggers = true,
        };

        // Counting process for the number of people in each room
        foreach (var room in ShipStatus.Instance.AllRooms)
        {
            var roomId = room.RoomId;
            // If it's not a passage or a hit-and-run, it's nothing
            if (roomId == SystemTypes.Hallway || room.roomArea == null)
            {
                continue;
            }
            // Number of hits detected The hits detected are stored here in colliders
            var numColliders = room.roomArea.OverlapCollider(filter, colliders);
            // Total number of bodies, including dead ones, actually displayed by admins
            var totalPlayers = 0;
            var numDeadBodies = 0;
            var numImpostors = 0;

            // Processing to each detected hit decision
            for (var i = 0; i < numColliders; i++)
            {
                var collider = colliders[i];
                // in the case of meat
                if (collider.CompareTag("DeadBody"))
                {
                    var deadBody = collider.GetComponent<DeadBody>();
                    if (deadBody != null && countedPlayers.Add(deadBody.ParentId))
                    {
                        totalPlayers++;
                        numDeadBodies++;
                        // If it the impostor's dead body
                        if (deadBody.ParentId.GetPlayer()?.Is(Custom_Team.Impostor) == true)
                        {
                            numImpostors++;
                        }
                    }
                }
                // If you are alive
                else if (!collider.isTrigger)
                {
                    var playerControl = collider.GetComponent<PlayerControl>();
                    if (playerControl.IsAlive() && countedPlayers.Add(playerControl.PlayerId))
                    {
                        totalPlayers++;
                        // If it was an Impostor
                        if (playerControl.Is(Custom_Team.Impostor))
                        {
                            numImpostors++;
                        }
                    }
                }
            }

            allAdmins[roomId] = new()
            {
                Room = roomId,
                TotalPlayers = totalPlayers,
                NumDeadBodies = numDeadBodies,
                NumImpostors = numImpostors,
            };
        }
        return allAdmins;
    }

    public readonly record struct AdminEntry
    {
        /// <summary>Subject room</summary>
        public SystemTypes Room { get; init; }
        /// <summary>Total players in the room</summary>
        public int TotalPlayers { get; init; }
        /// <summary>Number of dead bodies in the room</summary>
        public int NumDeadBodies { get; init; }
        /// <summary>Whether there is an impostor in the room</summary>
        public int NumImpostors { get; init; }
    }
}
