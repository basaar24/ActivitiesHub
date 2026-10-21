import { Box, Container, CssBaseline, Typography } from '@mui/material'
import axios from 'axios'
import { useState } from 'react'
import NavBar from './NavBar'
import ActivityDashboard from '../../features/activities/dashboard/ActivityDashboard'
import { useQuery } from '@tanstack/react-query'

function App() {
  const [selectedActivity, setSelectedActivity] = useState<
    Activity | undefined
  >(undefined)
  const [editMode, setEditMode] = useState(false)

  const { data: activities, isPending } = useQuery({
    queryKey: ['activities'],
    queryFn: async () => {
      const response = await axios.get<Activity[]>(
        'https://localhost:5001/api/v1/events',
      )
      return response.data
    },
  })

  const handleSelectActivity = (id: string) => {
    setSelectedActivity(activities!.find((act) => act.id === id))
  }

  const handleCancelSelectActivity = () => {
    setSelectedActivity(undefined)
  }

  const handleOpenForm = (id?: string) => {
    if (id) handleSelectActivity(id)
    else handleCancelSelectActivity()
    setEditMode(true)
  }

  const handleFormClose = () => {
    setEditMode(false)
  }

  const handleSubmitForm = (activity: Activity) => {
    // if (activity.id) {
    //   setSelectedActivity(activity)
    //   setActivities(
    //     activities.map((act) => (act.id === activity.id ? activity : act)),
    //   )
    // } else {
    //   const newActivity = { ...activity, id: activities.length.toString() }
    //   setSelectedActivity(newActivity)
    //   setActivities([...activities, newActivity])
    // }
    console.log(activity)
    setEditMode(false)
  }

  const handleDeleteActivity = (id: string) => {
    // setActivities(activities.filter((act) => act.id !== id))
    console.log(id)
  }

  return (
    <Box sx={{ bgcolor: '#eeeeee', minHeight: '100vh' }}>
      <CssBaseline />
      <NavBar openForm={handleOpenForm} />
      <Container maxWidth="xl" sx={{ mt: 3 }}>
        {!activities || isPending ? (
          <Typography>Loading...</Typography>
        ) : (
          <ActivityDashboard
            activities={activities}
            selectActivity={handleSelectActivity}
            cancelSelectActivity={handleCancelSelectActivity}
            selectedActivity={selectedActivity}
            editMode={editMode}
            openForm={handleOpenForm}
            closeForm={handleFormClose}
            submitForm={handleSubmitForm}
            deleteActivity={handleDeleteActivity}
          />
        )}
      </Container>
    </Box>
  )
}

export default App
