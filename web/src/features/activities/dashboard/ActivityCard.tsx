import {
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  Chip,
  Typography,
} from '@mui/material'
import { useActivities } from '../../../lib/hooks/useActivities'

type Props = {
  activity: Activity
}

export default function ActivityCard({ activity }: Props) {
  const { deleteActivity } = useActivities()

  return (
    <Card sx={{ borderRadius: 3 }}>
      <CardContent>
        <Typography variant="h5">{activity.title}</Typography>
        <Typography sx={{ color: 'text.secondary', mb: 1 }}>
          {activity.date}
        </Typography>
        <Typography variant="body2">{activity.description}</Typography>
        <Typography variant="subtitle1">
          {activity.city}/{activity.venue}
        </Typography>
      </CardContent>
      <CardActions
        sx={{ display: 'flex', justifyContent: 'space-between', pb: 2 }}
      >
        <Chip label={activity.category} variant="outlined" />
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            onClick={() => deleteActivity.mutateAsync(activity.id)}
            loading={deleteActivity.isPending}
            size="medium"
            variant="outlined"
            color="error"
          >
            Delete
          </Button>
          <Button onClick={() => {}} size="medium" variant="contained">
            View
          </Button>
        </Box>
      </CardActions>
    </Card>
  )
}
