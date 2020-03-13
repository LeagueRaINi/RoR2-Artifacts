using R2API.Utils;
using BepInEx;
using RoR2;

namespace Artifacts
{
    // TODO: manually hook UnityEngine.Resources.Load to replace the unfinished textures

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.raini.artifacts", "Artifacts", "1.0.0")]
    public class Artifacts : BaseUnityPlugin
    {
        public void Awake()
        {
            // lets not ruin peoples save files
            On.RoR2.UserProfile.Save += (orig, self, blocking) =>
            {
                return true;
            };

            // just incase they do add the artifacts we dont want duplicates in the unlockableDefTable and
            // since they dont check for those we will do that for them
            On.RoR2.UnlockableCatalog.RegisterUnlockable += (orig, name, unlockableDef) =>
            {
                if (UnlockableCatalog.GetUnlockableDef(name) != null)
                    return;

                orig(name, unlockableDef);
            };

            // add artifacts to unlocks
            On.RoR2.UnlockableCatalog.Init += (orig) =>
            {
                for (var artifactIdx = ArtifactIndex.Command; artifactIdx < ArtifactIndex.Count; artifactIdx++)
                {
                    var artifactDef = ArtifactCatalog.GetArtifactDef(artifactIdx);
                    if (artifactDef?.unlockableName.Length < 0)
                        continue;

                    Reflection.InvokeMethod(typeof(UnlockableCatalog), "RegisterUnlockable",
                        artifactDef.unlockableName, new UnlockableDef
                    {
                        hidden = true,
                    });
                }

                orig();
            };

            // set neat colors and tooltips for artifacts that have none
            On.RoR2.RuleDef.FromArtifact += (orig, artifactIdx) =>
            {
                var tooltipClr = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier3ItemDark);
                var artifactDef = ArtifactCatalog.GetArtifactDef(artifactIdx);

                var ruleDef = new RuleDef("Artifacts." + artifactIdx.ToString(), artifactDef.nameToken);
                {
                    var ruleChoiceDef1 = ruleDef.AddChoice("On", null, false);
                    ruleChoiceDef1.spritePath = artifactDef.smallIconSelectedPath;
                    ruleChoiceDef1.tooltipNameColor = tooltipClr;
                    ruleChoiceDef1.unlockableName = artifactDef.unlockableName;
                    ruleChoiceDef1.artifactIndex = artifactIdx;

                    var ruleChoiceDef2 = ruleDef.AddChoice("Off", null, false);
                    ruleChoiceDef2.spritePath = artifactDef.smallIconDeselectedPath;
                    ruleChoiceDef2.tooltipNameColor = tooltipClr;
                    ruleChoiceDef2.materialPath = "Materials/UI/matRuleChoiceOff";
                    ruleChoiceDef2.tooltipBodyToken = null;

                    switch (artifactIdx)
                    {
                        case ArtifactIndex.Bomb:
                            ruleChoiceDef1.tooltipNameToken = "Spite";
                            ruleChoiceDef2.tooltipNameToken = ruleChoiceDef1.tooltipNameToken;
                            ruleChoiceDef1.tooltipBodyToken = "Makes monsters explode on death";
                            ruleChoiceDef2.tooltipBodyToken = ruleChoiceDef1.tooltipBodyToken;
                            break;
                        case ArtifactIndex.Spirit:
                            ruleChoiceDef1.tooltipNameToken = "Spirit";
                            ruleChoiceDef2.tooltipNameToken = ruleChoiceDef1.tooltipNameToken;
                            ruleChoiceDef1.tooltipBodyToken = "Makes you and monsters move faster at low health";
                            ruleChoiceDef2.tooltipBodyToken = ruleChoiceDef1.tooltipBodyToken;
                            break;
                        case ArtifactIndex.Enigma:
                            ruleChoiceDef1.tooltipNameToken = "Enigma";
                            ruleChoiceDef2.tooltipNameToken = ruleChoiceDef1.tooltipNameToken;
                            ruleChoiceDef1.tooltipBodyToken = "Give you a special use item that triggers a random effect on use";
                            ruleChoiceDef2.tooltipBodyToken = ruleChoiceDef1.tooltipBodyToken;
                            break;
                        default:
                            ruleChoiceDef1.tooltipNameToken = "Work in Progress";
                            ruleChoiceDef2.tooltipNameToken = ruleChoiceDef1.tooltipNameToken;
                            ruleChoiceDef1.tooltipBodyToken = "This artifact does not do anything yet";
                            ruleChoiceDef2.tooltipBodyToken = ruleChoiceDef1.tooltipBodyToken;
                            break;
                    }
                }

                ruleDef.MakeNewestChoiceDefault();

                return ruleDef;
            };

            // unlock artifacts for the current user
            LocalUserManager.onUserSignIn += (user) =>
            {
                for (var artifactIdx = ArtifactIndex.Command; artifactIdx < ArtifactIndex.Count; artifactIdx++)
                {
                    var artifactDef = ArtifactCatalog.GetArtifactDef(artifactIdx);
                    if (artifactDef?.unlockableName.Length < 0)
                        continue;

                    var unlockableDef = UnlockableCatalog.GetUnlockableDef(artifactDef.unlockableName);
                    if (unlockableDef is null)
                    {
                        Logger.LogError($"Could not unlock artifact {artifactDef.nameToken}, " +
                            "UnlockableDef does not exist");
                        continue;
                    }

                    user.userProfile.GrantUnlockable(unlockableDef);
                }
            };
        }
    }
}
