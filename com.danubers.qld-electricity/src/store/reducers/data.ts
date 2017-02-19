/**
 * Created by daniel on 19/2/17.
 */
import {Action, Reducer} from 'redux'
import {EnergyData} from "../../types/data/energyData";
import {ActionCreator} from "react-redux";

export interface DataState {
    energy: EnergyData,
    loading: boolean
}

export enum Actions {
    LoadingData,
    LoadedData
}

export function initialState(): DataState {
    return {
        energy: null,
        loading: false
    }
}
export const reducer: Reducer<DataState> = (state: DataState = initialState(), action: any = {type: null}) => {
    switch (action.type) {
        case Actions.LoadingData:
            return {...state, loading: action.isLoading};
        case Actions.LoadedData:
            return {...state, loading: false, energy: action.payload};
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
        type: Actions.LoadedData,
        payload
    } as Action
}

