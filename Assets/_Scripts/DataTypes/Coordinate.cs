using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coordinate : IEquatable<Coordinate>
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    // Implement IEquatable<Coordinate>
    public bool Equals(Coordinate other)
    {
        if (other is null) return false;
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    // Override Equals for general object comparison
    public override bool Equals(object obj)
    {
        return Equals(obj as Coordinate);
    }

    // Override GetHashCode for use in dictionaries
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();
            hash = hash * 23 + Z.GetHashCode();
            return hash;
        }
    }
}
