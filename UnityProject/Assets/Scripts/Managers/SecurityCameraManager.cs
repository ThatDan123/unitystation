using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
	public class SecurityCameraManager : MonoBehaviour
	{
		private static SecurityCameraManager instance;

		public static SecurityCameraManager Instance => instance;

		private Dictionary<GameObject, SecurityCameraChannels> securityCameras = new Dictionary<GameObject, SecurityCameraChannels>();

		public Dictionary<GameObject, SecurityCameraChannels> SecurityCameras => securityCameras;

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		public void AddCamera(GameObject secCamera, SecurityCameraChannels channel)
		{
			securityCameras.Add(secCamera, channel);
		}

		public void RemoveCamera(GameObject secCamera)
		{
			securityCameras.Remove(secCamera);
		}
	}

	public enum SecurityCameraChannels
	{
		Station,
		Syndicate
	}
}
