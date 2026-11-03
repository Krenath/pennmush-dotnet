namespace PennMUSH.Core.Database;

using System.Collections.Generic;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ObjectDatabase
{
    private readonly List<MUSHObject?> _objects = new();

    public int Count => _objects.Count;
    public Dbref Top => new Dbref(_objects.Count);

    public void Clear() => _objects.Clear();

    public IEnumerable<MUSHObject> GetAllObjects() => _objects.Where(o => o != null)!;

    public void SetObject(MUSHObject obj)
    {
        while (_objects.Count <= obj.Id.Value)
        {
            _objects.Add(null);
        }
        _objects[obj.Id.Value] = obj;
    }

    public MUSHObject CreateObject(string name, ObjectType type, Dbref owner)
    {
        var id = new Dbref(_objects.Count);
        var obj = new MUSHObject(id, name, type, owner);
        _objects.Add(obj);
        return obj;
    }

    public MUSHObject? GetObject(Dbref id)
    {
        if (!id.IsValid || id.Value >= _objects.Count)
            return null;

        return _objects[id.Value];
    }

    public bool Contains(Dbref id)
    {
        return id.IsValid && id.Value < _objects.Count && _objects[id.Value] != null;
    }

    public bool DestroyObject(Dbref id)
    {
        if (!Contains(id))
            return false;

        var existing = _objects[id.Value];
        if (existing == null)
            return false;

        existing.Type = ObjectType.Garbage;
        existing.Name = "Garbage";
        return true;
    }

    public void MoveObject(Dbref targetRef, Dbref destinationRef)
    {
        var target = GetObject(targetRef);
        if (target == null) return;

        // Remove from old container
        if (target.Location.IsValid)
        {
            var oldLoc = GetObject(target.Location);
            if (oldLoc != null)
            {
                if (oldLoc.Contents == targetRef)
                {
                    oldLoc.Contents = target.Next;
                }
                else
                {
                    var currRef = oldLoc.Contents;
                    while (currRef.IsValid)
                    {
                        var curr = GetObject(currRef);
                        if (curr == null) break;
                        if (curr.Next == targetRef)
                        {
                            curr.Next = target.Next;
                            break;
                        }
                        currRef = curr.Next;
                    }
                }
            }
        }

        // Add to new container
        target.Location = destinationRef;
        if (destinationRef.IsValid)
        {
            var newLoc = GetObject(destinationRef);
            if (newLoc != null)
            {
                target.Next = newLoc.Contents;
                newLoc.Contents = targetRef;
            }
            else
            {
                target.Next = Dbref.Nothing;
            }
        }
        else
        {
            target.Next = Dbref.Nothing;
        }
    }

    public void AttachExit(Dbref roomRef, Dbref exitRef)
    {
        var room = GetObject(roomRef);
        var exit = GetObject(exitRef);
        if (room == null || exit == null) return;

        exit.Exits = roomRef; // Source room stored in exit.Exits
        exit.Next = room.Exits;
        room.Exits = exitRef;
    }

    public void RemoveExit(Dbref roomRef, Dbref exitRef)
    {
        var room = GetObject(roomRef);
        var exit = GetObject(exitRef);
        if (room == null || exit == null) return;

        if (room.Exits == exitRef)
        {
            room.Exits = exit.Next;
        }
        else
        {
            var currRef = room.Exits;
            while (currRef.IsValid)
            {
                var curr = GetObject(currRef);
                if (curr == null) break;
                if (curr.Next == exitRef)
                {
                    curr.Next = exit.Next;
                    break;
                }
                currRef = curr.Next;
            }
        }

        exit.Exits = Dbref.Nothing;
        exit.Next = Dbref.Nothing;
    }

    public void LinkExit(Dbref exitRef, Dbref destinationRef)
    {
        var exit = GetObject(exitRef);
        if (exit == null) return;

        exit.Location = destinationRef; // Exit destination stored in exit.Location
    }

    public IEnumerable<MUSHObject> GetContents(Dbref containerRef)
    {
        var container = GetObject(containerRef);
        if (container == null) yield break;

        var seen = new HashSet<Dbref>();
        var currRef = container.Contents;
        while (currRef.IsValid && seen.Add(currRef))
        {
            var curr = GetObject(currRef);
            if (curr == null) yield break;
            yield return curr;
            currRef = curr.Next;
        }
    }

    public IEnumerable<MUSHObject> GetExits(Dbref roomRef)
    {
        var room = GetObject(roomRef);
        if (room == null) yield break;

        var seen = new HashSet<Dbref>();
        var currRef = room.Exits;
        while (currRef.IsValid && seen.Add(currRef))
        {
            var curr = GetObject(currRef);
            if (curr == null) yield break;
            yield return curr;
            currRef = curr.Next;
        }
    }

    public MUSHObject? FindExit(Dbref roomRef, string nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias)) return null;
        string search = nameOrAlias.Trim();

        foreach (var exit in GetExits(roomRef))
        {
            // Exit name may contain aliases separated by ';' (e.g. "North;n;out")
            var aliases = exit.Name.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var alias in aliases)
            {
                if (alias.Equals(search, StringComparison.OrdinalIgnoreCase))
                    return exit;
            }
        }

        return null;
    }

    public MUSHObject? FindInContents(Dbref containerRef, string nameOrDbref)
    {
        if (string.IsNullOrWhiteSpace(nameOrDbref)) return null;
        string search = nameOrDbref.Trim();

        if (search.StartsWith('#') && int.TryParse(search.Substring(1), out int id))
        {
            var obj = GetObject(new Dbref(id));
            if (obj != null && obj.Location == containerRef) return obj;
        }

        foreach (var item in GetContents(containerRef))
        {
            if (item.Name.Equals(search, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        // Prefix match fallback
        foreach (var item in GetContents(containerRef))
        {
            if (item.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }
}