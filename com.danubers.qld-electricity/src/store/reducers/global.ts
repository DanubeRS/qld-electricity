/**
 * Created by daniel on 18/2/17.
 */
import {createStore, Action, combineReducers} from 'redux'

export interface GlobalState {
    test: Date
}

export const GLOBAL_TYPES  = {
    TEST_UPDATE_DATE: "GLOBAL_TEST_UPDATE_DATE"
};

function initialState() : GlobalState {
    return  {
        test: new Date()
    }
}

export const reducer = ((state: GlobalState = initialState(), action: Action = {type: null}) => {
    switch (action.type){
        case GLOBAL_TYPES.TEST_UPDATE_DATE:
        {
            return <GlobalState>{...state, test: new Date()}
        }
        default: {
            return state;
        }
    }
});

export function updateDate() : Action {
    return {
        type: GLOBAL_TYPES.TEST_UPDATE_DATE
    }
}