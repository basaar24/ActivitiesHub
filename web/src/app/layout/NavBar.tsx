import { Group } from '@mui/icons-material'
import {
  Box,
  AppBar,
  Toolbar,
  Typography,
  Container,
  MenuItem,
  MenuList,
  LinearProgress,
} from '@mui/material'
import { NavLink } from 'react-router'
import MenuItemLink from '../shared/components/MenuItemLink'
import { useStore } from '../../lib/hooks/useStore'
import { Observer } from 'mobx-react-lite'

export default function NavBar() {
  const { uiStore } = useStore()

  return (
    <Box sx={{ flexGrow: 1 }}>
      <AppBar
        position="static"
        sx={{
          backgroundImage:
            'linear-gradient(135deg, red 0%, blue 70%, green 90%)',
          position: 'relative',
        }}
      >
        <Container maxWidth="xl">
          <Toolbar sx={{ display: 'flex', justifyContent: 'space-between' }}>
            <MenuList>
              <MenuItem
                component={NavLink}
                to="/"
                sx={{ display: 'flex', gap: 2 }}
              >
                <Group fontSize="large" />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                  Activities
                </Typography>
              </MenuItem>
            </MenuList>
            <MenuList disablePadding sx={{ display: 'flex' }}>
              <MenuItemLink to="/activities">Activities</MenuItemLink>
              <MenuItemLink to="/createActivity">Create activity</MenuItemLink>
              <MenuItemLink to="/counter">Counter</MenuItemLink>
            </MenuList>
            User menu
          </Toolbar>
        </Container>
        <Observer>
          {() =>
            uiStore.isLoading ? (
              <LinearProgress
                color="secondary"
                sx={{
                  position: 'absolute',
                  bottom: 0,
                  left: 0,
                  right: 0,
                  height: 5,
                }}
              />
            ) : null
          }
        </Observer>
      </AppBar>
    </Box>
  )
}
