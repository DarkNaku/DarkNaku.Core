using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DarkNaku.Core;

public class Fader : SingletonBehaviour<Fader> {
	private float workTime = 0F;
	private float fadeTime = 0F;
	private bool _isWorking = false;
	private Color fromColor = Color.clear;
	private Color toColor = Color.clear;
	private readonly Color outColor = Color.black;
	private readonly Color inColor = Color.clear;

	private Canvas FadeCanvas { get; set; }
	private Image FadeImage { get; set; }

    protected override void OnInstantiate() {
        DontDestroyOnLoad(gameObject);

		name = "Fader";

        FadeCanvas = gameObject.AddComponent<Canvas>();
        FadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        FadeCanvas.enabled = false;
		FadeCanvas.sortingOrder = 1000;

		FadeImage = (new GameObject("Curtain")).AddComponent<Image>();
		FadeImage.transform.SetParent(transform);
		FadeImage.transform.localScale = Vector3.one;
		FadeImage.rectTransform.anchorMin = Vector2.zero;
		FadeImage.rectTransform.anchorMax = Vector2.one;
		FadeImage.rectTransform.offsetMin = Vector2.zero;
		FadeImage.rectTransform.offsetMax = Vector2.zero;
    }

	public static bool IsWorking => Instance._isWorking;

	public static Coroutine FadeIn(float duration, System.Action callback = null) {
		return Instance._FadeIn(duration, callback);
	}

	public static Coroutine FadeOut(float duration, System.Action callback = null) {
		return Instance._FadeOut(duration, callback);
	}

	private Coroutine _FadeIn(float duration, System.Action callback) {
        return StartCoroutine(CoFade(outColor, inColor, duration, () => {
			FadeCanvas.enabled = false;
			_isWorking = false;
			if (callback != null) callback();
		}));
	}

	private Coroutine _FadeOut(float duration, System.Action callback) {
		return StartCoroutine(CoFade(inColor, outColor, duration, () => {
			_isWorking = false;
			if (callback != null) callback();
		}));
	}

	private IEnumerator CoFade(Color from, Color to, float duration, System.Action callback) {
		workTime = 0F;
		fadeTime = duration;
		fromColor = from;
		toColor = to;
		FadeImage.color = from;

		if (_isWorking) yield break;

		_isWorking = true;
		FadeCanvas.enabled = true;

		while (FadeImage.color != toColor) {
			workTime += Time.deltaTime;
			FadeImage.color = Color.Lerp(fromColor, toColor, workTime / fadeTime);
			yield return null;
		}

		_isWorking = false;

		if (callback != null) callback();
	}
}