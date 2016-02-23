/** \file AdManager
 *  \brief Класс обеспечивает управление рекламой трех типов: видеоролики, баннеры и полноэкранные
 *         баннеры.  В функционал менеджера входит:
 *          - проверка кэширования рекламы
 *          - обработка основных событий рекламных сервисов
 *          - скрытие и показ баннера 
 *          - вручение награды за просмотр видеороликов 
 *          - управление приоритетом рекламных сервисов видеорекламы.
 *
 *         Требования: SDK Vungle, UniyADS, CharBoost, AdMob 
 *         Copyright (c) 2015-2016 GreenSnowGames.
 */

using UnityEngine;
using ChartboostSDK;
using UnityEngine.Advertisements;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;


public class AdManager : MonoBehaviour {
  // локальные переменные Vungle
  private Dictionary<string, object> VungleOptions = new Dictionary<string, object>(); 
  
  // локальные переменные AdMob
  private BannerView bannerView;
  private InterstitialAd interstitial;
  private string adUnitId;      
  
  // локальные переменные UnityAds
  private ShowOptions options;

  // Закрытое поле Одиночки
  private static AdManager _me;

  // NOTE: В "ленивой" реализации одиночки создание объекта происходит при первом обращении
  
  /// Открытое свойство Одиночки для доступа к полям и методам из других классов
  /**
   * Ленивая реализация паттерна Одиночка.
   * @return ссылка на Одиночку
   */
  public static AdManager me{
    get { return _me ?? (_me = new GameObject("BigAdManager").AddComponent<AdManager>()); }
  }

  // NOTE: Оператор ?? (сoalesce или просто "колеса" ) возвращает левый операнд, если он 
  // не null.  Иначе возвращается операнд, расположенный справа от колес.


  /// Перечисление с возможными наградами
  public enum Reward {
    NOTHING, ///< Ничего
    Coin,    ///< Монетки
    Bonus,   ///< Бонусный буст
    Lotto,   ///< Игра в лотерею
    Reborn   ///< Перерождение червя
  }
  /// Коробка с наградой
  Reward rewardType = Reward.NOTHING;

  /// Перечисление с возможными рекламными сервисами
  public enum AdService {
    EMPTY,
    Charboost,
    Vungle,
    UnityAds
  }

  /// Готовая к показу реклама хранится здесь
  AdService preparedAdService = AdService.EMPTY;

  /// Метод, выполняющийся после создания объекта
  /**
   * Подготовка рекламного менеджера к работе
   * Здесь заканчивается воскресенье, и начинаются суровые будни понедельника
   */
  private void Start (){
    // Не удалять объект, перед загрузкой другой сцены
    GameObject.DontDestroyOnLoad(this.gameObject); 

    // Инициализация всех рекламных сервисов
    me.InitAdServices();

    // Активация делегатов
    me.InitializeEvents();

    // NOTE: Ключевые слова в начале Debug.Log упрощают процесс отладки с помощью logcat
    Debug.Log ("DESU: AdManager started");
  }



  /// Инициализация рекламных сервисов
  /**
   * Закрытый метод для инициализации всех рекламных сервисов. Здесь настраиваются параметры
   * для рекламы: КЛЮЧИ, области показа, и дополнительные опции. 
   */
  private void InitAdServices(){
    // Настройка Charboost
    // NOTE:  Ключ инициализации Charboost прописывается в Editor. В коде его нет.

    // Автоматический запуск кэширования после просмотра очередного видео       
    // NOTE: В доках пишут, что эта опция включена по умолчанию
    Chartboost.setAutoCacheAds(true);

    // NOTE: Нет ничего лучше, чем включить автокэширование, а потом закэшировать видео самому
    // Вручную закэшиовать видео в зону в область показа Default
    Chartboost.cacheRewardedVideo(CBLocation.Default);
    
    // Вручную закэшировать полноэкранный баннер в зону в область показа Default
    Chartboost.cacheInterstitial(CBLocation.Default);

    // Настройка Vungle  
    // За видео дается награда (его можно отменять)
    VungleOptions.Add("incentivized", true);   

    // Для Android - true обозначает, что Vungle пытается подогнать видео под текущую ориентацию
    VungleOptions.Add("orientation", true);     
    
    // Большие кнопки
    VungleOptions.Add("large", true);    
           
    // Подпись в шапке "Предостережения о закрытии обозревателя рекламы" (ПоЗОР)
    VungleOptions.Add("alertTitle", "Careful!"); 

    // Полное сообщение в ПоЗОР
    VungleOptions.Add("alertText", 
    "If the video isn't completed you won't get reward! Are you sure you want to close early?");

    // Надпись на кнопке "Закрыть" в ПоЗОР
    VungleOptions.Add("closeText", "Close");

    // Надпись на кнопке "Продолжить просмотр" в ПоЗОР
    VungleOptions.Add("continueText", "Keep Watching");

    // На весь экран
    VungleOptions.Add("immersive", true);

    // Персональные ключи из личного кабинета. 
    // NOTE: Первый для Android, второй для iOS - ощутите, кто больший параноик
    Vungle.init("000XX00X000XX00X0000000", "777777777");
    

    // Настройка UnityAds 

    // NOTE: #if UNITY_ANDROID - это директива препроцессора для плотформенно зависимой компиляции.
    // Код из блока #if UNITY_ANDROID будет передан компилятору в случае, если в BuildSetings
    // выставлена платформа Android
    
    #if UNITY_ANDROID
      // Ключи из личного кабинета UnityADS для приложения под Android
      Advertisement.Initialize("77771", false); 
    #else
      // Ключи из личного кабинета UnityADS для приложения под iOS
      Advertisement.Initialize("77772", false);
    #endif


    // Настройка AdMob
    // Метод, который просит AdMob снабдить червя баннером
    BannerRequest();
    // Снабжаем еще и полноэкранным баннером
    RequestAdMobInterstitial();

    Debug.Log("DESU: AdManager intialized");
  }


  // NOTE: Регионы очень удобны, когда нужно обозначить часть кода с общими признаками
  #region PUBLIC_METHODS

  /// Проверить, готова ли видеореклама для показа
  /**
   * Заряжатель реклам. Возвращает истину, если есть хотя бы одна закэшированная 
   * реклама, после чего записывает номер закэшированной рекламы в preparedAdService.
   * @return True, если есть закэшированная реклама. Иначе - False.
   */
  public bool IsReady(){

    // Если реклама уже заряжена
    if (preparedAdService != AdService.EMPTY) 
      return true;

    // NOTE: Обязательно убедитесь в названии области показа рекламы для UnityADS. Название 
    // области можно посмотреть личном кабинете. Последнее время она стала называться 
    // rewardedVideo. Без этого не будет работать <b>даже</b> в Editor

    if (Chartboost.hasRewardedVideo(CBLocation.Default)) { 
      preparedAdService = AdService.Charboost;
      return true;
    } else if (Vungle.isAdvertAvailable()) { 
      preparedAdService = AdService.Vungle;
      return true;
    } else  if (Advertisement.IsReady("rewardedVideoZone")) {
      preparedAdService = AdService.UnityAds;
      return true;
    } 
    
    // Если рекламы нет
    return false;

    // NOTE: Этот последний false, говорит либо о банальном отсутствии интернета, либо о том, 
    // что реклама кончилась. Каждый сервис дает примерно 20 роликов в сутки на пользователя.
  }

  /// Показать рекламу
  /**
   * Метод запускает вывод закэшированной видеорекламы на экран. Сервис, для запуска определяется 
   * по атрибуту preparedAdService, который предварительно задается в методе IsReady. 
   * @param r Вариант награды
   */
  public void ShowAD(byte r){
    // Запоминаем награду.
    rewardType = r;

    // Иногда необходимо проверить конкретный сервис. Для этого нужно раскомментировать строку ниже.
    // preparedAdService = 1; 
 
    switch (preparedAdService) {
    case AdService.Charboost:
      Chartboost.showRewardedVideo(CBLocation.Default);
      break;
    case AdService.Vungle:
      Vungle.playAdWithOptions(VungleOptions);
      break;
    case AdService.UnityAds:
      OnAdOpen();
      Advertisement.Show("rewardedVideoZone", options);
      break;
    default:
      break;
    }

    // NOTE: У UnityADS нет Event'a, который бы выстреливал перед показом видео, поэтому OnAdOpen()
    // для UnityADS вызываем вручную перед показом.

    // Разрядить рекламострел.
    preparedAdService = 0; 
  }

  #endregion

  // Далее идет регион с закрытыми методами, которые будут прописаны в делегаты. Использовать эти
  // методы непосредственно как делегаты не получится, потому что у каждого SDK свой интерфейс
  // Event'a, не смотря на то, что отлавливают подобные события: закрытие, открытие видео итд.
  #region PRIVATE_METHODS

  /// Подготовиться к старту видео
  /**
   * Метод, описывающий логику, которая должна выполняться перед запуском видеоролика.
   * Обычно нужен для выключения музыки или паузы.
   */
  private void OnAdOpen(){ 
    AudioListener.pause = true;
  }

  /// Логика по завершению ролика
  /**
   *  Метод запускается после показа рекламы. Независимо от того было ли видео досмотрено до 
   *  конца. Нужен для включения обратно всего того, что было выключено в OnAdOpen
   */
  private void OnAdClose(){ 
    AudioListener.pause = false;
  }


  /// Выдать награду
  /**
   * Метод выдает награду на основании параметра rewardType, заданного в ShowAD. Этот метод 
   * участвует в делегатах, анализирующих время просмотренного видео. Этот метод необходимо вызвать
   * в случае если ролик был просмотрен до конца.
   */
  private void GiveReward(){ 
    switch (rewardType) {
    case Reward.Coin:
      AddCoins();
      break;  
    case Reward.Bonus:
      AddBonus();
      break;
    case Reward.Lotto:
      LoadLoto();
      break;
    case Reward.Reborn:
      Reborn();
      break;
    default:
      break;
    } 
  }
  #endregion


  #region BANNERS
  // NOTE: Ключи ниже - из официальной документации по AdMob. Удобно для тестов
  // Баннер: ca-app-pub-3940256099942544/6300978111  
  // Полноэкранный баннер: ca-app-pub-3940256099942544/1033173712

  /// Запросить баннер
  /**
   * Инициализирует баннеры AdMob в соответствии с ключом из личного кабинета. После инициализации
   * создает bannerView и загружает туда баннер. После загрузки баннер скрывается.
   */
  private void BannerRequest() {
    #if UNITY_EDITOR
      adUnitId = "unused";
    #elif UNITY_ANDROID
      adUnitId = "ca-app-pub-3940256099942544/6300978111";
    #elif UNITY_IOS
      adUnitId = "ca-app-pub-3940256099942544/6300978111";
    #else
      adUnitId = "unexpected_platform";
    #endif

    // adUnitId - ключ, 
    // AdSize.SmartBanner - размер баннера (автоматический),   
    // AdPosition.Bottom - местоположение (прицеплен к нижней части экрана)
    bannerView = new BannerView(adUnitId, AdSize.SmartBanner, AdPosition.Bottom);

    // Загрузка изображения в экземпляр баннера
    bannerView.LoadAd(new AdRequest.Builder().Build());

    // Баннер нужно спрятать после инициализации
    bannerView.Hide();
  }

  /// Запросить баннер
  /**
   * Инициализирует полноэкранные баннеры (interstitial) AdMob в соответствии с ключом из 
   * личного кабинета. 
   */
  private void RequestAdMobInterstitial() {
    #if UNITY_ANDROID
      string adUnitId = "ca-app-pub-3940256099942544/1033173712";
    #elif UNITY_IPHONE
      string adUnitId = "ca-app-pub-3940256099942544/1033173712";
    #else
      string adUnitId = "unexpected_platform";
    #endif
    interstitial = new InterstitialAd(adUnitId);
    interstitial.LoadAd(new AdRequest.Builder().Build());
  }


  /// Открытый метод для "тяжелого" показа баннера
  /**
   * Показ баннера, совмещенный с инициализацией. Каждый раз при показе создается новый объект, в
   * который загружается изображение баннера. После инициализации изображение выводится на экран.  
   * На случай, если боитесь, что во время игры доступ к интернет сначала пропадет, а потом 
   * появится, но баннер проинициализироваться не успеет.
   */
  public void BannerHardShow() {
    BannerRequest();
    bannerView.Show();
  }

  /// Открытый метод для "легкого" показа баннера
  /**
   * Обычный показ. Если во время инициализации не было интернета - баннера не будет
   */
  public void BannerShow() {
    bannerView.Show();
  }

  /// Открытый метод скрытия баннера
  /**
   * Скрыть баннер. Иногда мы это делаем. Мы же любим своих игроков.
   */
  public void BannerHide() {
    bannerView.Hide();
  }

  /// Открытый метод показа полноэкранного баннера AdMob
  /**
   * Разворачивает на весь экран рекламное изображение
   */
  public void ShowAdMobInterstitial() {
    // Перед показом баннера необходимо убедиться, что он успешно закэширован
    if (interstitial.IsLoaded()) {
      interstitial.Show();
    } 
  }

  /// Открытый метод показа полноэкранного баннера Charboost
  /**
   * Разворачивает на весь экран рекламное изображение
   */
  public void ShowCharboostInterstitial() {
    // Перед показом баннера необходимо убедиться, что он успешно закэширован
    if (Chartboost.hasInterstitial(CBLocation.Default)) {
      Chartboost.showInterstitial(CBLocation.Default);
    }
  }


  #endregion

  // Ниже идет блок делегатов для каждого рекламного сервиса. 
  // Каждый сервис имеет свою небольшую специфику, но в целом логика работы очень похожа.
  #region VunleEvents

  /// Открытый метод-делегат для события  Vungle.onAdStartedEvent 
  /**
   * Перед открытием рекламы выполняет метод OnAdOpen();
   */
  private void OnAdStartedEvent(){
    OnAdOpen();  
  }


  /// Открытый метод-делегат для события  Vungle.onAdFinished  
  /**
   * Метод проверяет, досмотрена ли реклама до конца и в зависимости от результата, может 
   * вручить награду
   * @param a Тип события по завершению рекламы
   */
  private void OnAdFinished( AdFinishedEventArgs a ) {
    // Если игрок полностью посмотрел видео-ролик
    if (a.IsCompletedView) {
      GiveReward();
    }

    // Если игрок нажал на кнопку INSTALL в конце рекламы
    if (a.WasCallToActionClicked) {
      // NOTE: Ни в коем случае не поощряйте игрока за нажатия кнопки "скачать" . 
      // Если это выяснится - сразу забанят в Vungle. Максимум тут можно отправлять статистику 
      // на GoogleAnalytics или любой другой сервис аналитики.
    }

    // Очищаем переменную с типом рекламы, даже если её не выдали.
    rewardType = Reward.NOTHING;

    // У Vungle нет обработки события закрытия рекламы, поэтому в конец прописываем OnAdClose()
    OnAdClose();  
  }


  /// Метод-делегат для события  Vungle.onCachedAdAvailableEvent  
  /**
   * Метод проверяет, закэшировалась ли реклама. В случае, есди это так выставим preparedAdService 
   * на значение Vungle, обозначив готовность этого сервиса к показу видеоролика.
   * @param isPlayable закэшировалась ли реклама Vungle
   */
  private void OnCachedAdAvailableEvent(bool isPlayable) {
    if (isPlayable) {
      preparedAdService = AdService.Vungle;
    }
  }
  #endregion


  #region CharBoostEvents

  /// Метод-делегат для события Chartboost.didDisplayRewardedVideo 
  /**
   * Перед открытием рекламы выполняет метод OnAdOpen();
   * @param location Область отображения рекламы
   */
  private void DidDisplayRewardedVideo(CBLocation location){ 
    OnAdOpen();  
  }

  /// Метод-делегат для события Chartboost.didDismissRewardedVideo  
  /**
   * После закрытия рекламы выполняет метод OnAdClose();
   * @param location Область отображения рекламы
   */
  private void DidDismissRewardedVideo(CBLocation location){
    OnAdClose();
  }

  /// Метод-делегат для события Chartboost.didCompleteRewardedVideo
  /**
   * Метод вручает награду, в случае если реклама досмотрена до конца
   * @param location Область отображения рекламы
   * @param rew Тип награды
   */
  private void DidCompleteRewardedVideo(CBLocation location, int rew ){
    GiveReward();
    // Обнуляем значение типа награды
    rewardType = Reward.NOTHING; 
  }


  /// Метод-делегат для события  Vungle.onCachedAdAvailableEvent
  /**
   * Метод проверяет, закэшировалась ли реклама. Если да, то выставить preparedAdService на 
   * Charboost, обозначая готовность сервиса к просмотру.
   * @param location Область отображения рекламы
   */
  private void DidCacheRewardedVideo(CBLocation location) {
    preparedAdService = AdService.Charboost;
  }
  #endregion

  #region UnityADSEvent
  /// Метод-делегат для UnityADS
  /**
   * Метод проверяет, результат, полученный после просмотра видео, в зависимости от исхода:
   * реклама просмотрена / реклама пропущена / рекламу не получилось загрузить,
   * назначаем соответствующее поведение. 
   * @param result Результат просмотра рекламы.
   */
  private void UAShowResult( ShowResult result ) {
    switch (result) {
      // Если видео досмотрено до конца
      case ShowResult.Finished:
        // Debug.Log("The ad was successfully shown.");
        GiveReward();
        rewardType = Reward.NOTHING;
        OnAdClose();
        break;
      // Если видео НЕ досмотрено до конца
      case ShowResult.Skipped:   
        // Debug.Log("The ad was skipped before reaching the end.");
        OnAdClose();
        break;
      // Если видело не загрузилось
      case ShowResult.Failed:
        // Debug.LogError("The ad failed to be shown.");
        OnAdClose();
        break;
    }
  }
  #endregion

  #region AdMobEvents

  /// Метод-делегат для AdMob
  /**
   * Метод запускает кэширование следующего баннера после закрытия текущего
   * реклама просмотрена / реклама пропущена / рекламу не получилось загрузить,
   * назначаем соответствующее поведение. 
   * @param sender 
   * @param args
   */
  private void OnInterstitialAsMobClosed( object sender, EventArgs args ) {
      interstitial.LoadAd(request);
    }
  #endregion

  // Ниже описывается блок с наградами
  #region  rewardTypeEvents  

  private void AddCoins(){ 
    // Добавить монет
  }

  private void AddBonus(){ 
    // Добавить бонусов
  }

  private void LoadLoto() {
    // Сыграть в лото
  }

  private void Reborn() { 
     // Переродиться
  }

  #endregion

  /// Метод пред-деструктора
  /**
   * Выполняется перед уничтожением объекта
   */
  private void OnDestroy(){
    // Довольно странный метод, учитывая, что этот объект собирается жить всю игру
    DeInitializeEvents();
  }

  /// Иницниализация делегатов
  /**
   * Подписываются методы на эвенты из других SDK. Когда эвент выстрелит - он запустит метод,
   * который подписан на него. Здесь вся магия. 
   */
  private void InitializeEvents() {

    // Vungle 
    // Срабатывает перед запуском рекламы
    Vungle.onAdStartedEvent  += OnAdStartedEvent; 
        
    //Срабатывает после успешного просмотра
    Vungle.onAdFinishedEvent += OnAdFinished;

    // Срабатывает после того как реклама прокэшировалась    
    Vungle.adPlayableEvent   += OnCachedAdAvailableEvent;


    // Charboost
    // NOTE: Возможных событий гораздо больше - обязательно ознакомьтесь с официальной документацией
    // Срабатывает если видео закрыто,но не просмотрено до конца
    Chartboost.didDismissRewardedVideo  += DidDismissRewardedVideo;  
   
    // Срабатывает, если видео успешно просомтерно
    Chartboost.didCompleteRewardedVideo += DidCompleteRewardedVideo; 
    
    // Срабатывает перед запуском видео
    Chartboost.didDisplayRewardedVideo  += DidDisplayRewardedVideo; 
    
    // Срабатывает после окончания кэширования видоса
    Chartboost.didCacheRewardedVideo    += DidCacheRewardedVideo;


    // UntiyADS
    // Настройка параметров для видеорекламы UnityAds. 
    // Один из параметров - делегат, возвращающий результат просмотра видео
    options = new ShowOptions { resultCallback = UAShowResult };

    // AdMob
    interstitial.AdClosed += OnInterstitialAsMobClosed;

    Debug.Log ("DESU: AdManager Listening");
  }


  /// Де-Иницниализация делегатов.
  /**
   * Отписываемя от событий. Это необходимо делать при уничтожении объекта, иначе 
   * подписка делегата на событие может остаться, хотя объекта уже не будет.  
   */
  private void DeInitializeEvents() {
    // Отписываемся от эвентов Vungle
    Vungle.onAdStartedEvent  -= OnAdStartedEvent;
    Vungle.onAdFinishedEvent -= OnAdFinished;
    Vungle.adPlayableEvent   -= OnCachedAdAvailableEvent;

    // Отписывается от эвентов Chartboost
    Chartboost.didDismissRewardedVideo  -= DidDismissRewardedVideo;
    Chartboost.didCompleteRewardedVideo -= DidCompleteRewardedVideo;
    Chartboost.didDisplayRewardedVideo  -= DidDisplayRewardedVideo;
    Chartboost.didCacheRewardedVideo    -= DidCacheRewardedVideo;

    // Отписываемся от эвента AdMob
    interstitial.AdClosed -= OnInterstitialAsMobClosed;

  }


}
