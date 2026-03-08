using UnityEngine;
using System.Collections;

public class WeatherSystem : MonoBehaviour
{
    public enum WeatherType
    {
        Sunny,
        Rain,
        Cloudy,
        Storm,
        ShootingStar
    }

    [Header("Timing")]
    public float weatherDuration = 180f;

    [Header("Particle Systems")]
    public ParticleSystem rainParticles;
    public ParticleSystem stormParticles;
    public ParticleSystem shootingStarParticles;

    [Header("Lighting")]
    public Light sunLight;

    public Color sunnyColor = Color.white;
    public Color cloudyColor = new Color(0.7f, 0.7f, 0.7f);
    public Color stormColor = new Color(0.5f, 0.5f, 0.6f);
    public Color nightColor = new Color(0.3f, 0.3f, 0.45f);

    public float sunnyIntensity = 1.2f;
    public float cloudyIntensity = 0.8f;
    public float stormIntensity = 0.4f;
    public float nightIntensity = 0.25f;

    private WeatherType currentWeather;

    void Start()
    {
        StartCoroutine(WeatherLoop());
    }

    IEnumerator WeatherLoop()
    {
        while (true)
        {
            WeatherType nextWeather =
                (WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);

            SetWeather(nextWeather);

            yield return new WaitForSeconds(weatherDuration);
        }
    }

    void SetWeather(WeatherType type)
    {
        currentWeather = type;

        rainParticles.Stop();
        stormParticles.Stop();
        shootingStarParticles.Stop();

        switch (type)
        {
            case WeatherType.Sunny:

                sunLight.color = sunnyColor;
                sunLight.intensity = sunnyIntensity;

                break;

            case WeatherType.Cloudy:

                sunLight.color = cloudyColor;
                sunLight.intensity = cloudyIntensity;

                break;

            case WeatherType.Rain:

                rainParticles.Play();
                sunLight.color = cloudyColor;
                sunLight.intensity = cloudyIntensity;

                break;

            case WeatherType.Storm:

                stormParticles.Play();
                sunLight.color = stormColor;
                sunLight.intensity = stormIntensity;

                break;

            case WeatherType.ShootingStar:

                shootingStarParticles.Play();
                sunLight.color = nightColor;
                sunLight.intensity = nightIntensity;

                break;
        }

        Debug.Log("Weather changed to: " + type);
    }
}