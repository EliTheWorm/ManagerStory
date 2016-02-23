/** \file LanguageClient
 *  \brief Класс для обновления языкового спрайта на объекте.
 *
 *         Требования: LanguageManager, Callback, Messenger
 *         http://wiki.unity3d.com/index.php?title=Advanced_CSharp_Messenger
 *
 *         Copyright (c) 2015-2016 GreenSnowGames.
 */

using UnityEngine;
using System.Collections;

public class LanguageClient : MonoBehaviour {

  // Кэш SpriteRenderer на случай, если языки придется менять часто во время игры
  SpriteRenderer sr;

  // Кэш текущего имени спрайта
  string pic_name;

  /// Инициализация языкового клиента
  /**
   * Кэшируется SpriteRendere, имя языкового спрайта.
   * Язык заменяет на текущий, выставленный в LanguageManager
   */
  void Start () {
    // Устанавливаем слушатель для события Обновление языка
    Messenger.AddListener("RefreshLanguage", RefreshLanguage);
    // Кэшируем SpriteRendere
    sr = GetComponent<SpriteRenderer>();
    // Узнаем имя спрайта, который СЕЙЧАС висит на объекте
    pic_name = sr.sprite.name;
    // Ищем плашку с тем же именем, но из другого языка
    sr.sprite = Resources.Load<Sprite>("lang/" + LanguageManager.me.CurrentLanguage + "/"+ pic_name);
	}


  /// Метод пред-деструктора
  /**
   * Выполняется перед уничтожением объекта. 
   * Снимает слушатель с объекта.
   */
  void OnDestroy() {
    	Messenger.RemoveListener("RefreshLanguage", RefreshLanguage);
	}


  /// Делегат слушателя для обновления языка
  /**
   * Изменяет языковой спрайт на другой, соответствующий измененному языку
   * Если кто-то запустит Messenger.Broadcast("RefreshLanguage"); то выполнится 
   * RefreshLanguage каждого объекта на котором закреплен LanguageClient 
   */
  void RefreshLanguage() {
    // Ищем плашку с тем же именем, но из другого языка
    // Подразумевается, что перед вызовом LANG.me.CurrentLanguage был изменен
    sr.sprite = Resources.Load<Sprite>("lang/" + LanguageManager.me.CurrentLanguage + "/" + pic_name);
	}
}
