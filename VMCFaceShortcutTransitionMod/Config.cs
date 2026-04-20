using Newtonsoft.Json;
using System.Collections.Generic;

namespace VMCFaceShortcutTransitionMod
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Config
    {
        [JsonProperty] public bool Enable = true;
        [JsonProperty] public float DefaultTransitionSec = 0.1f;
        [JsonProperty] public int TickMs = 16;
        [JsonProperty] public bool ApplyOnlyWhenSoftChangeFalse = true;
        [JsonProperty] public List<ActionRule> ActionRules = new List<ActionRule>();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ActionRule
    {
        [JsonProperty] public string ActionName = string.Empty;
        [JsonProperty] public bool Enable = true;
        [JsonProperty] public float TransitionSec = 0.2f;
    }
}
