namespace Memento_Grupo1.Memento
{
    public class Memento
    {
        public string State { get; set; }

        public Memento(string state)
        {
            State = state;
        }
    }
}
