using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// The slide itself: a short-lived component on the boat, alive only on the machine that
    /// owns it, that carries the hull toward the water and then removes itself.
    ///
    /// Why a velocity controller and not one impulse. A beached hull is held by ground
    /// friction, which is a deceleration and does not depend on mass, so a single kick that
    /// would move a karve stops a longship dead in a metre, and one big enough for the
    /// longship launches the karve. Steering the horizontal velocity toward a target speed
    /// each physics step, with the change capped by Acceleration, is indifferent to both:
    /// the speed is the design (scaled by hull in Shoving), and friction only decides how
    /// much of the cap it eats. Vertical velocity is never touched, so gravity and the
    /// ground keep their say and the hull follows the slope.
    ///
    /// Only the owner runs this. Writes to a body you do not own are overwritten by the
    /// owner's next physics sync, which is the silent kind of failure; Shoving routes the
    /// request to whoever owns the boat. It stops the moment the hull is over floating
    /// depth, and from then on Ship.CustomFixedUpdate's buoyancy and the player's rowing are
    /// the game's, with whatever momentum is left.
    /// </summary>
    internal sealed class Shove : MonoBehaviour
    {
        private Ship _ship;
        private Rigidbody _body;
        private Vector3 _direction;
        private float _speed;
        private float _timeLeft;
        private float _age;
        private Vector3 _checkpoint;
        private float _checkpointAge;

        internal static bool Active(Ship ship)
        {
            return ship != null && ship.GetComponent<Shove>() != null;
        }

        internal static void Begin(Ship ship, Vector3 direction, float speed, float seconds)
        {
            var body = ship.GetComponent<Rigidbody>();
            if (body == null) return;

            var shove = ship.GetComponent<Shove>();
            if (shove == null) shove = ship.gameObject.AddComponent<Shove>();

            shove._ship = ship;
            shove._body = body;
            shove._direction = direction;
            shove._speed = speed;
            shove._timeLeft = seconds;
            shove._age = 0f;
            shove._checkpoint = body.position;
            shove._checkpointAge = 0f;
        }

        private void FixedUpdate()
        {
            if (_ship == null || _body == null) { Destroy(this); return; }

            if (!_ship.IsOwner()) { Stop("ownership moved"); return; }
            if (_ship.HasPlayerOnboard()) { Stop("someone boarded"); return; }

            var dt = Time.fixedDeltaTime;
            _age += dt;
            _timeLeft -= dt;

            if (Water.Afloat(_body.worldCenterOfMass)) { Stop("afloat"); return; }
            if (_timeLeft <= 0f) { Stop("out of time"); return; }

            // Jammed against a rock or a tree: the same position for a second and a half
            // means the controller is only straining the body, and it would keep doing so
            // until the clock ran out.
            if (_age - _checkpointAge > 1.5f)
            {
                if ((_body.position - _checkpoint).magnitude < 0.3f) { Stop("stuck"); return; }

                _checkpoint = _body.position;
                _checkpointAge = _age;
            }

            var velocity = _body.linearVelocity;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            var wanted = _direction * _speed;
            var change = Vector3.MoveTowards(flat, wanted, FleytaConfig.Acceleration.Value * dt) - flat;

            _body.AddForce(change, ForceMode.VelocityChange);
        }

        private void Stop(string why)
        {
            if (FleytaConfig.Verbose.Value)
                FleytaPlugin.Log.LogInfo("Shove ended after " + _age.ToString("0.0") + "s: " + why + ".");

            Destroy(this);
        }
    }
}
