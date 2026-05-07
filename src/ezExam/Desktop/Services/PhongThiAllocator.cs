using System;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Services
{
    public enum RoomAllocationStrategy
    {
        MaxMerge,
        SequentialSbd
    }

    public class SubjectCount
    {
        public string Subject { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RoomEntry
    {
        public string Subject { get; set; } = string.Empty;
        public int Count { get; set; }
        public int StartSbd { get; set; }
        public int EndSbd { get; set; }
    }

    public class RoomPlan
    {
        public int RoomNumber { get; set; }
        public List<RoomEntry> Entries { get; set; } = new();
        public int Total => Entries.Sum(e => e.Count);
    }

    public class PhongThiAllocator
    {
        public List<RoomPlan> Allocate(List<SubjectCount> subjects, int roomCapacity, RoomAllocationStrategy strategy)
        {
            if (subjects == null || subjects.Count == 0) return new List<RoomPlan>();
            if (roomCapacity <= 0) throw new ArgumentException("roomCapacity must be > 0");

            return strategy == RoomAllocationStrategy.MaxMerge
                ? AllocateMaxMerge(subjects, roomCapacity)
                : AllocateSequentialSbd(subjects, roomCapacity);
        }

        private static List<RoomPlan> AllocateSequentialSbd(List<SubjectCount> subjects, int roomCapacity)
        {
            var queue = subjects
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .Select(s => new SubjectCount { Subject = s.Subject, Count = s.Count })
                .ToList();

            var rooms = new List<RoomPlan>();
            var nextSbd = 1;
            var roomNo = 1;

            while (queue.Any(s => s.Count > 0))
            {
                var room = new RoomPlan { RoomNumber = roomNo++ };
                var remaining = roomCapacity;

                foreach (var subject in queue)
                {
                    if (subject.Count <= 0 || remaining <= 0) continue;

                    var take = Math.Min(subject.Count, remaining);
                    room.Entries.Add(new RoomEntry
                    {
                        Subject = subject.Subject,
                        Count = take,
                        StartSbd = nextSbd,
                        EndSbd = nextSbd + take - 1
                    });

                    nextSbd += take;
                    subject.Count -= take;
                    remaining -= take;
                }

                rooms.Add(room);
            }

            return rooms;
        }

        private static List<RoomPlan> AllocateMaxMerge(List<SubjectCount> subjects, int roomCapacity)
        {
            var ordered = subjects
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .Select(s => new SubjectCount { Subject = s.Subject, Count = s.Count })
                .ToList();

            var rooms = new List<RoomPlan>();
            var nextSbd = 1;
            var roomNo = 1;

            while (ordered.Any(s => s.Count >= roomCapacity))
            {
                var subject = ordered.First(s => s.Count >= roomCapacity);
                var room = new RoomPlan { RoomNumber = roomNo++ };
                room.Entries.Add(new RoomEntry
                {
                    Subject = subject.Subject,
                    Count = roomCapacity,
                    StartSbd = nextSbd,
                    EndSbd = nextSbd + roomCapacity - 1
                });
                nextSbd += roomCapacity;
                subject.Count -= roomCapacity;
                rooms.Add(room);
            }

            while (ordered.Any(s => s.Count > 0))
            {
                var room = new RoomPlan { RoomNumber = roomNo++ };
                var remaining = roomCapacity;

                foreach (var subject in ordered.Where(s => s.Count > 0).OrderBy(s => s.Count))
                {
                    if (remaining == 0) break;
                    var take = Math.Min(subject.Count, remaining);
                    room.Entries.Add(new RoomEntry
                    {
                        Subject = subject.Subject,
                        Count = take,
                        StartSbd = nextSbd,
                        EndSbd = nextSbd + take - 1
                    });
                    nextSbd += take;
                    subject.Count -= take;
                    remaining -= take;
                }

                rooms.Add(room);
            }

            return rooms;
        }
    }
}
