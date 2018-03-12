using UnityEngine;
using System.Collections;

namespace Jobs
{
    public class MasterQueueManager : Singleton<MasterQueueManager>
    {
        public string[] StartQueues = new string[] { "task_queue" };
        QueueHandler queueHandler = new QueueHandler();

        // Use this for initialization
        void Start()
        {
            Debug.Log("Connecting To Queues");
            var masterRouter = MasterProtoRouter.Instance;
            queueHandler.ConnectQueues(queues: StartQueues);
        }

        // Update is called once per frame
        void Update()
        {

        }
        public override void OnDestroy()
		{
            base.OnDestroy();
            queueHandler = null;
		}
	}
}