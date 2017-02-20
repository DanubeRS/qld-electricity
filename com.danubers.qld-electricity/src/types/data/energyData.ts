import {EnergyDataPoint, WeatherDataPoint} from "./energyDataPoint";
/**
 * Created by daniel on 19/2/17.
 **/

export interface EnergyData {
    start: Date;
    end: Date;
    points: EnergyDataPoint[]
}

export interface WeatherData {
    start: Date;
    end: Date;
    points: WeatherDataPoint[]
}
