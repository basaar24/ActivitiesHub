import { useEffect, useState } from "react"

function App() {
  const [activities, setActivities] = useState<Activity[]>([]);

  useEffect(() => {
    fetch('https://localhost:5001/api/v1/events')
      .then(response => response.json())
      .then(data => setActivities(data));

    return () => {};
  }, []);

  return (
    <div>
      <h3 style={{ color: 'red' }}>Events Hub</h3>
      <ul>
        {activities.map((activity: Activity) => (
          <li key={activity.id}>{activity.title}</li>
        ))}
      </ul>
    </div>
  )
}

export default App
