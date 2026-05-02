using UnityEngine;

interface IHackable
{
    void Hack(float duration);
    bool IsHacked { get; }

}
