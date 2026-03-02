using UnityEngine;

[CreateAssetMenu(fileName = "AnimationsMonster", menuName = "Scriptable Objects/AnimationsMonster")]
public class AnimationsMonster : ScriptableObject
{
    const string prefix = "monster_";

    public string idle = prefix + "idle";
    public string run = prefix + "run";
    public string stun = prefix + "stun";
    public string shock = prefix + "shock";

}
