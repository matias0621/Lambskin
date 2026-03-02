using UnityEngine;

[CreateAssetMenu(fileName = "AnimationsHuman", menuName = "Scriptable Objects/AnimationsHuman")]
public class AnimationsHuman : ScriptableObject
{
    const string prefix = "human_";

    public string idle = prefix + "idle";
    public string walk = prefix + "walk";
    public string attack = prefix + "attack";
    
}
