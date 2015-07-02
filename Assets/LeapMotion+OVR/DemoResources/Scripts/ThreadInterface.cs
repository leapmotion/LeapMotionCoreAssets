using System.Threading;
using System.Collections;

public class ThreadInterface {

  private Thread m_thread = null;
  private object m_handle = new object();
  private bool m_isFinished = false;

  public bool IsFinished
  {
    get
    {
      bool temp;
      lock (m_handle)
      {
        temp = m_isFinished;
      }
      return temp;
    }

    set
    {
      lock (m_handle)
      {
        m_isFinished = value;
      }
    }
  }

  protected virtual void ThreadFunction() { }

  protected virtual void OnFinished() { }

  private void Run()
  {
    ThreadFunction();
    m_isFinished = true;
  }

  public virtual void Abort()
  {
    m_thread.Abort();
  }

	public virtual void Start () {
    m_thread = new Thread(Run);
    m_thread.Start();
	}
	
	
	public virtual bool Update () {
    if (IsFinished)
    {
      OnFinished();
      return true;
    }
    return false;
	}
}
