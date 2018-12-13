using System;
using System.Threading.Tasks;

namespace BridgeBotNext.Providers
{
 public abstract class Provider
 {
  protected Provider() {}

  /**
   * Provider unique name
   */
  public virtual string Name => "";

  /**
   * Provider display name
   */
  public virtual string DisplayName => "";

  /**
   * Start provider and connect to the messaging service 
   */
  public abstract Task Connect();

  /*
   * Disconnect from the messaging service and stop provider
   */
  public abstract Task Disconnect();

  /**
   * Send message to provider
   */
  public abstract Task SendMessage(Conversation conversation, Message message);


  public class MessageEventArgs : EventArgs
  {
   public Message Message { get; }

   public MessageEventArgs(Message message)
   {
    Message = message;
   }
  }

  /**
   * Message received event
   */
  public event EventHandler<MessageEventArgs> MessageReceived;
  
  protected virtual void OnMessageReceived(MessageEventArgs e)
  {
   // Make a temporary copy of the event to avoid possibility of
   // a race condition if the last subscriber unsubscribes
   // immediately after the null check and before the event is raised.
   var handler = MessageReceived;
   handler?.Invoke(this, e);
  }


 }
}