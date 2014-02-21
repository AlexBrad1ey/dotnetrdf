using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VDS.RDF.Query.Spin.Statistics
{

    /**
     * A singleton managing statistics for SPIN execution.
     * In TopBraid, this singleton is used as a single entry point for various
     * statistics producing engines such as TopSPIN.
     * The results are displayed in the SPIN Statistics view of TBC.
     * 
     * The SPINStatisticsManager is off by default, and needs to be activated
     * with <code>setRecording(true);</code>.
     * 
     * @author Holger Knublauch
     */
    public class SPINStatisticsManager
    {

        private static SPINStatisticsManager _singleton = new SPINStatisticsManager();

        /**
         * Gets the singleton instance of this class.
         * @return the SPINStatisticsManager (never null)
         */
        public static SPINStatisticsManager get()
        {
            return _singleton;
        }


        private HashSet<ISPINStatisticsListener> _listeners = new HashSet<ISPINStatisticsListener>();

        private bool _recording;

        private bool _recordingNativeFunctions;

        private bool _recordingSPINFunctions;

        //TODO: check for thread safety and synchronization. If order is not an issue, use System.Collections.Concurrent.ConcurrentBag instead
        private List<SPINStatistics> stats = new List<SPINStatistics>();


        public void addListener(ISPINStatisticsListener listener)
        {
            _listeners.Add(listener);
        }


        /**
         * Adds new statistics and notifies any registered listeners.
         * This should only be called if <code>isRecording()</code> is true
         * to prevent the unnecessary creation of SPINStatistics objects.
         * @param values  the statistics to add
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void add(IEnumerable<SPINStatistics> values)
        {
            addSilently(values);
            notifyListeners();
        }


        /**
         * Adds new statistics without notifying listeners.
         * This should only be called if <code>isRecording()</code> is true
         * to prevent the unnecessary creation of SPINStatistics objects.
         * @param values  the statistics to add
         */
        public void addSilently(IEnumerable<SPINStatistics> values)
        {
            foreach (SPINStatistics s in values)
            {
                stats.Add(s);
            }
        }


        /**
         * Gets all previously added statistics.
         * @return the statistics
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<SPINStatistics> getStatistics()
        {
            return stats;
        }


        public bool isRecording()
        {
            return _recording;
        }


        public bool isRecordingNativeFunctions()
        {
            return _recordingNativeFunctions;
        }


        public bool isRecordingSPINFunctions()
        {
            return _recordingSPINFunctions;
        }


        public void removeListener(ISPINStatisticsListener listener)
        {
            _listeners.Remove(listener);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void reset()
        {
            stats.Clear();
            notifyListeners();
        }


        /**
         * Notifies all registered SPINStatisticsListeners so that they can refresh themselves.
         */
        public void notifyListeners()
        {
            foreach (ISPINStatisticsListener listener in new List<ISPINStatisticsListener>(_listeners))
            {
                listener.statisticsUpdated();
            }
        }


        public void setRecording(bool value)
        {
            this._recording = value;
        }


        public void setRecordingNativeFunctions(bool value)
        {
            this._recordingNativeFunctions = value;
        }


        public void setRecordingSPINFunctions(bool value)
        {
            this._recordingSPINFunctions = value;
        }
    }
}