import { Grid } from "@mui/material";
import ActivityList from "./ActivityList";
import ActivityDetail from "../details/ActivityDetail";

type Props = {
    activities: Activity[];
}

export default function ActivityDashboard({ activities }: Props) {
    return (
        <Grid container spacing={3}>
            <Grid size={7}>
                <ActivityList activities={activities} />
            </Grid>
            <Grid size={5}>
                {activities[2] && <ActivityDetail activity={activities[7]} />}
            </Grid>
        </Grid>
    )
}