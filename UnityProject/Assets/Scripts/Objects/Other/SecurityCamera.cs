using System;
using Managers;
using UnityEngine;

namespace Objects.Other
{
	public class SecurityCamera : MonoBehaviour
	{
		public SecurityCameraChannels cameraChannel = SecurityCameraChannels.Station;

		private void OnEnable()
		{
			SecurityCameraManager.Instance.AddCamera(gameObject, cameraChannel);
		}

		private void OnDisable()
		{
			SecurityCameraManager.Instance.RemoveCamera(gameObject);
		}
	}
}
