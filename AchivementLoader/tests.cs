#if DEBUG
using RoR2;
using RoR2.Achievements;

namespace Dak.AchievementLoader
{
    [CustomUnlockable("Example.Example", "ACHIEVEMENT_EXAMPLEACHIEVEMENT_DESCRIPTION")]
    [RegisterAchievement("ExampleAchievement", "Example.Example", null, null)]
    class tests : BaseAchievement
    {
        public override void OnInstall()
        {
            base.OnInstall();
            RoR2Application.onUpdate += AutoGrant;
        }

        public override void OnUninstall()
        {
            base.OnUninstall();
            RoR2Application.onUpdate -= AutoGrant;
        }

        public void AutoGrant()
        {
            Grant();
        }
    }
}
#endif