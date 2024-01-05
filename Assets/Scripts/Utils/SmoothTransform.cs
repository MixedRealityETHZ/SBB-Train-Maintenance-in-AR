#region

using System;
using UnityEngine;

#endregion

namespace Utils
{
	/// <summary>
	///     Smoothly interpolates GameObject positions. Use the `SetPosition` method instead of updating the transform
	///     directly.
	/// </summary>
	public class SmoothTransform : MonoBehaviour
	{
		[Tooltip("Duration in seconds of smooth transition.")]
		public float smoothingTime = 0.666f;

		[Tooltip("Interpolation function between start and target positions")]
		public AnimationCurve smoothingFunction = AnimationCurve.EaseInOut(0, 0, 1, 1);

		private Vector3 _smoothPosition = Vector3.zero;
		private Quaternion _smoothRotation = Quaternion.identity;
		private Vector3 _smoothScale = Vector3.one;
		private Vector3 _startPosition;
		private Quaternion _startRotation;
		private Vector3 _startScale;
		private float _time;

		private void Start()
		{
			var t = transform;
			_smoothPosition = _startPosition = t.position;
			_smoothRotation = _startRotation = t.rotation;
			_smoothScale = _startScale = t.localScale;
		}

		private void Update()
		{
			if (_time < 0 || _time >= smoothingTime) return;

			var t = smoothingFunction.Evaluate(_time / smoothingTime);
			transform.position = Vector3.Lerp(_startPosition, _smoothPosition, t);
			transform.rotation = Quaternion.Lerp(_startRotation, _smoothRotation, t);
			transform.localScale = Vector3.Lerp(_startScale, _smoothScale, t);

			_time = Math.Clamp(_time + Time.deltaTime, 0, smoothingTime);
		}

		private void ResetStartTransform()
		{
			_time = 0;
			// Set start point to current transform
			_startPosition = transform.position;
			_startRotation = transform.rotation;
			_startScale = transform.localScale;
		}

		/// <summary>
		///     Set new target position of this transform. Script will then smoothly transform the object to the new
		///     position.
		/// </summary>
		/// <param name="position">Target position</param>
		/// <param name="instantaneous">Set to true to disable interpolation</param>
		public void SetPosition(Vector3 position, bool instantaneous = false)
		{
			if (instantaneous)
			{
				_smoothPosition = _startPosition = transform.position = position;
			}
			else
			{
				ResetStartTransform();
				_smoothPosition = position;
			}
		}

		/// <summary>
		///     Set new target rotation of this transform. Script will then smoothly transform the object to the new
		///     rotation.
		/// </summary>
		/// <param name="rotation">Target rotation</param>
		/// <param name="instantaneous">Set to true to disable interpolation</param>
		public void SetRotation(Quaternion rotation, bool instantaneous = false)
		{
			if (instantaneous)
			{
				_smoothRotation = _startRotation = transform.rotation = rotation;
			}
			else
			{
				ResetStartTransform();
				_smoothRotation = rotation;
			}
		}

		/// <summary>
		///     Set new target scale of this transform. Script will then smoothly transform the object to the new
		///     scale.
		/// </summary>
		/// <param name="scale">Target scale</param>
		/// <param name="instantaneous">Set to true to disable interpolation</param>
		public void SetScale(Vector3 scale, bool instantaneous = false)
		{
			if (instantaneous)
			{
				_smoothScale = _startScale = transform.localScale = scale;
			}
			else
			{
				ResetStartTransform();
				_smoothScale = scale;
			}
		}
	}
}