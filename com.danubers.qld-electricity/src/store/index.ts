/**
 * Created by daniel on 19/2/17.
 */

import {combineReducers} from 'redux'
import {reducer as globalReducer, GlobalState} from './reducers/global'
import {reducer as dataReducer, DataState} from "./reducers/data";

export default combineReducers({
    global: globalReducer,
    data: dataReducer
})

export interface State {
    global: GlobalState,
    data: DataState
}
