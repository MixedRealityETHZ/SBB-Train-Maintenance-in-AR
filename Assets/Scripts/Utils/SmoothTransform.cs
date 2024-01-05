#region

using System;
using UnityEngine;

#endregion

namespace Utils
{
	public class SmoothTransform : MonoBehaviour
	{
		public float smoothingTime = 0.666f;

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

		// public Vector3 SmoothPosition
		// {
		// 	get { return smoothPosition; }
		// 	set
		// 	{
		// 		ResetStartTransform();
		// 		smoothPosition = value;
		// 	}
		// }
		//
		// public Vector3 SmoothRotation
		// {
		// 	get { return smoothRotation; }
		// 	set
		// 	{
		// 		ResetStartTransform();
		// 		smoothRotation = value;
		// 	}
		// }
		//
		// public Vector3 SmoothScale
		// {
		// 	get { return smoothScale; }
		// 	set
		// 	{
		// 		ResetStartTransform();
		// 		smoothScale = value;
		// 	}
		// }

		private void ResetStartTransform()
		{
			_time = 0;
			// Set start point to current transform
			_startPosition = transform.position;
			_startRotation = transform.rotation;
			_startScale = transform.localScale;
		}

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