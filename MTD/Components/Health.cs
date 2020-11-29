using ImGuiNET;
using Microsoft.Xna.Framework;
using Nez;
using Nez.ImGuiTools;
using Nez.ImGuiTools.ObjectInspectors;
using System;

namespace MTD.Components
{
    public class Health : Component
    {
        public event Action<Health, string> UponDeathEvent;
        public event Action<Health, int, string> UponHealthChangedEvent;

        /// <summary>
        /// The maximum health this entity can have.
        /// </summary>
        [NotInspectable]
        public int MaxHealth
        {
            get
            {
                return _maxHealth;
            }
            set
            {
                if (value == _maxHealth)
                    return;

                _maxHealth = value;
                if (_maxHealth < 1)
                    _maxHealth = 1;
                if (_currentHealth > _maxHealth)
                    CurrentHealth = _maxHealth;
            }
        }

        /// <summary>
        /// The current health this entity has. Can never be less than zero or greater than <see cref="MaxHealth"/>.
        /// </summary>
        [NotInspectable]
        public int CurrentHealth
        {
            get
            {
                return _currentHealth;
            }
            protected set
            {
                if (_currentHealth <= 0 && !AllowHealAfterDeath)
                    return;

                if (value == _currentHealth)
                    return;

                if (value < 0)
                    _currentHealth = 0;
                else if (value > _maxHealth)
                    _currentHealth = _maxHealth;
                else
                    _currentHealth = value;
            }
        }
        /// <summary>
        /// The current health percentage, on the zero to one scale.
        /// Also see <see cref="IsDead"/>.
        /// </summary>
        public float HealthPercentage
        {
            get
            {
                return (float)_currentHealth / _maxHealth;
            }
        }
        /// <summary>
        /// If true, then when this entity reaches zero health then 
        /// </summary>
        public bool DestroyUponDeath = true;
        public bool AllowHealAfterDeath = false;
        public bool IsDead
        {
            get
            {
                return _currentHealth <= 0 || Entity.IsNullOrDestroyed();
            }
        }

        private int _maxHealth = 100;
        private int _currentHealth = 100;

        public Health(int startHealth, int maxHealth)
        {
            this.MaxHealth = maxHealth;
            this._currentHealth = Mathf.Clamp(startHealth, 0, maxHealth);
        }

        private static int healthChangeImGui;
        [InspectorDelegate]
        private void DrawInspector()
        {
            var color = Vector4.Lerp(Color.Red.ToVector4(), Color.Green.ToVector4(), HealthPercentage);

            ImGui.Text("Health: ");
            ImGui.SameLine();
            ImGui.TextColored(color.ToNumerics(), $"{_currentHealth} / {_maxHealth}");

            int curr = CurrentHealth;
            ImGui.InputInt("Current:", ref curr);
            CurrentHealth = curr;
            int max = MaxHealth;
            ImGui.InputInt("Max:", ref max);
            MaxHealth = max;
            ImGui.Text($"{UponDeathEvent?.GetInvocationList()?.Length ?? 0} death listeners, {UponHealthChangedEvent?.GetInvocationList()?.Length ?? 0} change listeners.");

            if (ImGui.Button("Change Health"))
            {
                ChangeHealth(healthChangeImGui, "Dev Console");
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("Amount", ref healthChangeImGui, 10);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            UponDeathEvent = null;
            UponHealthChangedEvent = null;
        }

        public virtual bool ChangeHealth(int change, string dealer)
        {
            if (change == 0)
                return false;

            int start = CurrentHealth;
            CurrentHealth += change;
            bool changed = CurrentHealth != start;
            if (changed)
                UponHealthChange(dealer, start, CurrentHealth);
            return changed;
        }

        protected virtual void UponHealthChange(string dealer, int oldValue, int newHealthValue)
        {
            UponHealthChangedEvent?.Invoke(this, newHealthValue - oldValue, dealer);

            if (newHealthValue == 0)
                UponDeath(dealer);
        }

        protected virtual void UponDeath(string dealer)
        {
            UponDeathEvent?.Invoke(this, dealer);
            if (DestroyUponDeath)
            {
                this.Entity?.Destroy();
            }
        }
    }
}
