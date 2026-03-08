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

    void Awake()
    {
        if (rainParticles == null)
            rainParticles = transform.Find("RainParticles")?.GetComponent<ParticleSystem>();

        if (stormParticles == null)
            stormParticles = transform.Find("StormParticles")?.GetComponent<ParticleSystem>();

        if (shootingStarParticles == null)
            shootingStarParticles = transform.Find("ShootingStarParticles")?.GetComponent<ParticleSystem>();
    }

    void Start()
    {
        if (rainParticles != null) rainParticles.Stop();
        if (stormParticles != null) stormParticles.Stop();
        if (shootingStarParticles != null) shootingStarParticles.Stop();

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

        if (rainParticles != null) rainParticles.Stop();
        if (stormParticles != null) stormParticles.Stop();
        if (shootingStarParticles != null) shootingStarParticles.Stop();

        switch (type)
        {
            case WeatherType.Sunny:
                if (sunLight != null)
                {
                    sunLight.color = sunnyColor;
                    sunLight.intensity = sunnyIntensity;
                }
                break;

            case WeatherType.Cloudy:
                if (sunLight != null)
                {
                    sunLight.color = cloudyColor;
                    sunLight.intensity = cloudyIntensity;
                }
                break;

            case WeatherType.Rain:
                if (rainParticles != null) rainParticles.Play();
                if (sunLight != null)
                {
                    sunLight.color = cloudyColor;
                    sunLight.intensity = cloudyIntensity;
                }
                break;

            case WeatherType.Storm:
                if (stormParticles != null) stormParticles.Play();
                if (sunLight != null)
                {
                    sunLight.color = stormColor;
                    sunLight.intensity = stormIntensity;
                }
                break;

            case WeatherType.ShootingStar:
                if (shootingStarParticles != null) shootingStarParticles.Play();
                if (sunLight != null)
                {
                    sunLight.color = nightColor;
                    sunLight.intensity = nightIntensity;
                }
                break;
        }

        Debug.Log("Weather changed to: " + type);
    }
}