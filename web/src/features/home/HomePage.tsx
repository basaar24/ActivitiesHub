import { Group } from '@mui/icons-material'
import { Box, Button, Paper, Typography } from '@mui/material'
import { useNavigate } from 'react-router'

export default function HomePage() {
  const navigate = useNavigate()

  return (
    <Paper
      sx={{
        color: 'white',
        display: 'flex',
        flexDirection: 'column',
        gap: 6,
        alignItems: 'center',
        alignContent: 'center',
        justifyContent: 'center',
        height: '100vh',
        backgroundImage: 'linear-gradient(135deg, red 0%, blue 70%, green 90%)',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          alignContent: 'center',
          color: 'white',
          gap: 3,
        }}
      >
        <Group sx={{ height: 110, width: 110 }} />
        <Typography variant="h1">Acctivities Hub</Typography>
      </Box>
      <Typography variant="h2">Welcome</Typography>
      <Button
        onClick={() => navigate('/activities')}
        size="large"
        variant="contained"
        sx={{ height: 80, borderRadius: 4, fontSize: '1.5rem' }}
      >
        Let's go!
      </Button>
    </Paper>
  )
}
