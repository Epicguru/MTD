using JDef;
using System;

namespace MTD.Jobs
{
    public class GoalProviderDef : Def
    {
        public Type Class;

        public override void Validate()
        {
            base.Validate();

            if (Class == null)
            {
                ValidateError("Class is null.");
            }
            else if (!typeof(GoalProvider).IsAssignableFrom(Class))
            {
                ValidateError($"Class '{Class.FullName}' does not inherit from GoalProvider.");
                Class = null;
            }
        }

        public virtual GoalProvider Create()
        {
            if (Class == null)
                return null;

            var instance = Activator.CreateInstance(Class, this);
            return instance as GoalProvider;
        }
    }
}
