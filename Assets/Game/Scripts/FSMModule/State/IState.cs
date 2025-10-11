namespace FSMModule
{
    public interface IState
    {
        public void OnEnter();
        public void OnUpdate(float deltaTime);
        public void OnExit();
    }
}