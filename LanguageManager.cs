/** \file LanguageManager
 *  \brief Класс для управления языками в игре. Не шрифтовое решение.
 *
 *         Требования: Перед использованием этого класса нужно в папке Resources создать подпапки 
 * со спрайтами переведенными на другие языки. Имя папок должно соответствовать именам языков из 
 * SystemLanguage без пробелов. Имена спрайтов должны быть одинаковыми для разных языков.
 *
 *         Copyright (c) 2015-2016 GreenSnowGames.
 */


// NOTE: Решение не шрифтовое, а спрайтовое. С одной стороны, оно требовательно к памяти. 
// С другой стороны, позволяет создать свой дизайн надписи на любом языке и при этом не
// связываться со шрифтами.
// И вообще ...я ведь же всего лишь червь.

// Как же меня прет от этих зеленых букв.

using UnityEngine;
using System.Collections;

public class LanguageManager : MonoBehaviour {
  // "Вавилонский" класс SystemLanguage содержащий все языки
  public SystemLanguage CurrentLanguage; 

  //Стандартая уже практика одиночки. Ощущаете?
  public static LanguageManager me { get; private set; }

  /// Инициализации Одиночки
  /**
   * В методе проверяется - существует ли уже экземпляр класса на сцене
   * После инициализации в игре устанавливается язык системы. Если язык системы не предусмотрен
   * файлами ресурсов, то устанавливается английский язык. Данные о языке сохраняются в PlayerPrefs.
   */
  void Awake() {

    GameObject.DontDestroyOnLoad(this.gameObject); 
    if (me != null && me != this) {
      Destroy(gameObject);		
    } 
    me = this;

		// Проверяем есть ли переменная lang в Настройках
    if (PlayerPrefs.HasKey("lang")) {
      // Если есть, то присваиваем в язык переменную из Настроек
      CurrentLanguage = (SystemLanguage)PlayerPrefs.GetInt("lang");
    } else {
		  // Если нет, ищем сначала, существует ли такой язык в спрайтах
			// NOTE: Предполагается, что спрайт с именем "yes" есть во всех папках с языками
      if (Resources.Load<Sprite>("lang/" + Application.systemLanguage + "/yes")) {
        // Если результат успешен, то мы выставляем текущий язык системы
        CurrentLanguage = Application.systemLanguage;
      } else {
			  // Иначе выставляем английский
        CurrentLanguage = SystemLanguage.English;
      }
      // Сохраняем в настройки
      PlayerPrefs.SetInt("lang", (int)CurrentLanguage);
    }       
  }
}

