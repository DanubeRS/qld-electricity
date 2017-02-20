/**
 * Created by daniel on 19/2/17.
 */
import {Action, Reducer} from 'redux'
import {EnergyData, WeatherData} from "../../types/data/energyData";
import {ActionCreator} from "react-redux";

export interface DataState {
    energy: EnergyData,
    weather: WeatherData,
    loading: boolean
}

export enum Actions {
    LoadingData,
    LoadedWeatherData,
    LoadedEnergyData
}

export function initialState(): DataState {
    return {
        energy: null,
        weather: null,
        loading: false
    }
}
export const reducer: Reducer<DataState> = (state: DataState = initialState(), action: any = {type: null}) => {
    switch (action.type) {
        case Actions.LoadingData:
            return {...state, loading: action.isLoading};
        case Actions.LoadedEnergyData:
            return {...state, loading: false, energy: action.payload};
        case Actions.LoadedWeatherData:
            return {...state, loading: false, weather: action.payload};
        default: {
            return state;
        }
    }
};

export function loadingEnergyData(loading: boolean = true): Action {
    return {
        type: Actions.LoadingData,
        isLoading: loading
    } as Action
}

export function loadedEnergyData(payload: EnergyData) : Action {
    return {
        type: Actions.LoadedEnergyData,
        payload
    } as Action
}
export function loadedWeatherData(payload: WeatherData) :Action{
    return {
        type: Actions.LoadedWeatherData,
        payload
    } as Action;
}

