using Cysharp.Threading.Tasks;
public interface IBgmService
{
    UniTask Play(string id);
    UniTask Stop();
}

