public interface ISceneDirector
{
    void Load(string sceneName);
    void Load(int buildIndex);
    void ReloadCurrent();
    void NextScene();
    void PreviousScene();
}
