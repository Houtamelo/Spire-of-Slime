using UnityEngine;
using UnityEngine.UI;

namespace Unused.Free_UI_build_package.Script
{
	public class Slide : MonoBehaviour
	{
		private Image _filler;
		public Slider slider;

		// Use this for initialization
		private void Start ()
		{
			_filler = GetComponent<Image>();
		}
	
		// Update is called once per frame
		private void Update ()
		{
			_filler.fillAmount = slider.value;
		}
	}
}
